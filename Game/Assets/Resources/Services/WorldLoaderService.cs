using LuceRPG.Game.Models;
using LuceRPG.Models;
using Microsoft.FSharp.Collections;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace LuceRPG.Game.Services
{
    public class WorldLoaderService
    {
        public IEnumerator LoadWorld(Action onLoad)
        {
            if (!Registry.Stores.World.HasWorld())
            {
                yield return Registry.Services.Comms.LoadWorld(
                    payload => Registry.Stores.World.LoadFrom(payload),
                    e => Debug.LogError(e)
                );
            }

            onLoad();
        }

        public IEnumerator GetUpdates(
            Action<WorldDiffModule.DiffType> onDiff)
        {
            if (!Registry.Stores.World.HasWorld())
            {
                throw new Exception("No world in the store");
            }

            void OnUpdate(WithTimestamp.Model<GetSinceResultModule.Payload> update)
            {
                if (update.value.IsEvents)
                {
                    var events =
                        ((GetSinceResultModule.Payload.Events)update.value)
                        .Item
                        .Select(e => e.value)
                        .ToArray();

                    if (events.Any())
                    {
                        Registry.Streams.WorldEvents.NextMany(events, UpdateSource.Server);
                    }
                }
                else
                {
                    var worldUpdate =
                        ((GetSinceResultModule.Payload.World)update.value)
                        .Item;

                    CheckConsistency(onDiff, worldUpdate);
                }
            }

            void OnConsistencyCheck(WorldModule.Model world)
            {
                CheckConsistency(onDiff, world);
            }

            yield return Registry.Services.Comms.FetchUpdates(15, 0.1f, OnUpdate, OnConsistencyCheck);
        }

        private void CheckConsistency(
            Action<WorldDiffModule.DiffType> onDiff,
            WorldModule.Model world)
        {
            var diffs = WorldDiffModule.diff(world, Registry.Stores.World.World).ToArray();
            if (diffs.Any())
            {
                Debug.LogWarning($"Consistency check failed with {diffs.Length} results");
                var logs = ClientLogEntryModule.Payload.NewConsistencyCheckFailed(ListModule.OfSeq(diffs));
                Registry.Processors.Logs.AddLog(logs);
            }

            foreach (var diff in diffs)
            {
                onDiff(diff);
            }
        }
    }
}
