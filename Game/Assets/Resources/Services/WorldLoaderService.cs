using LuceRPG.Game.Models;
using LuceRPG.Models;
using LuceRPG.Utility;
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
        public static IEnumerator LoadWorld(Action onLoad)
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

        public static IEnumerator GetUpdates(
            Action<WithId.Model<WorldObjectModule.Payload>> onAddObject,
            Action<string, WorldEventModule.Model> onUcEvent,
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
                        ApplyUpdate(onAddObject, onUcEvent, events, UpdateSource.Server);
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

        private static void ApplyUpdate(
            Action<WithId.Model<WorldObjectModule.Payload>> onAddObject,
            Action<string, WorldEventModule.Model> onUcEvent,
            IEnumerable<WorldEventModule.Model> worldEvents,
            UpdateSource source
        )
        {
            foreach (var worldEvent in worldEvents)
            {
                if (source == UpdateSource.Server
                    && OptimisticIntentionProcessor.Instance.DidProcess(worldEvent.resultOf))
                {
                    var log = ClientLogEntryModule.Payload.NewUpdateIgnored(worldEvent);
                    LogDispatcher.Instance.AddLog(log);
                    OptimisticIntentionProcessor.Instance.CheckEvent(worldEvent);
                    continue;
                }

                Registry.Stores.World.Apply(worldEvent);

                var tObjectId = WorldEventModule.getObjectId(worldEvent.t);
                if (tObjectId.HasValue())
                {
                    var objectId = tObjectId.Value;

                    if (worldEvent.t.IsObjectAdded)
                    {
                        var objectAdded = ((WorldEventModule.Type.ObjectAdded)worldEvent.t).Item;
                        onAddObject(objectAdded);
                    }
                    else
                    {
                        onUcEvent(objectId, worldEvent);
                    }
                }
            }
        }

        private static void CheckConsistency(
            Action<WorldDiffModule.DiffType> onDiff,
            WorldModule.Model world)
        {
            var diffs = WorldDiffModule.diff(world, Registry.Stores.World.World).ToArray();
            if (diffs.Any())
            {
                Debug.LogWarning($"Consistency check failed with {diffs.Length} results");
                var logs = ClientLogEntryModule.Payload.NewConsistencyCheckFailed(ListModule.OfSeq(diffs));
                LogDispatcher.Instance.AddLog(logs);
            }

            foreach (var diff in diffs)
            {
                onDiff(diff);
            }
        }
    }
}
