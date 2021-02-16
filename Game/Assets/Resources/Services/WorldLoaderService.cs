using LuceRPG.Game.Models;
using LuceRPG.Models;
using Microsoft.FSharp.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
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
            Action<WorldEventModule.Model> onEvent,
            Action<WorldModule.Payload, IReadOnlyCollection<WorldDiffModule.DiffType>> onDiff)
        {
            if (!Registry.Stores.World.HasWorld())
            {
                throw new Exception("No world in the store");
            }

            void OnUpdate(WithTimestamp.Model<GetSinceResultModule.Payload> update)
            {
                Registry.Stores.World.LastUpdate = update.timestamp;

                if (update.value.IsEvents)
                {
                    var events =
                        ((GetSinceResultModule.Payload.Events)update.value)
                        .Item
                        .Select(e => e.value)
                        .ToArray();

                    if (events.Any())
                    {
                        foreach (var we in events)
                        {
                            onEvent(we);
                        }
                    }
                }
                else
                {
                    var worldUpdate =
                        ((GetSinceResultModule.Payload.World)update.value)
                        .Item.value;

                    CheckConsistency(onDiff, worldUpdate);
                }
            }

            void OnConsistencyCheck(WorldModule.Payload world)
            {
                CheckConsistency(onDiff, world);
            }

            yield return Registry.Services.Comms.FetchUpdates(15, 0.1f, OnUpdate, OnConsistencyCheck);
        }

        private void CheckConsistency(
            Action<WorldModule.Payload, IReadOnlyCollection<WorldDiffModule.DiffType>> onDiff,
            WorldModule.Payload world)
        {
            var diffs = WorldDiffModule.diff(Registry.Stores.World.World, world).ToArray();
            if (diffs.Any())
            {
                Debug.LogWarning($"Consistency check failed with {diffs.Length} results");
                var logs = ClientLogEntryModule.Payload.NewConsistencyCheckFailed(ListModule.OfSeq(diffs));
                Registry.Processors.Logs.AddLog(logs);

                onDiff(world, diffs);
            }
        }
    }
}
