﻿using LuceRPG.Game.Models;
using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LuceRPG.Game.Processors
{
    public class OptimisticIntentionProcessor : Processor<IndexedIntentionModule.Model>
    {
        public override float PollPeriod => 0.01f;

        private FSharpMap<string, long> _objectBusyMap
            = MapModule.Empty<string, long>();

        private readonly Dictionary<string, long> _intentions
            = new Dictionary<string, long>();

        private readonly Dictionary<string, Dictionary<int, WorldEventModule.Model>> _eventsProduced
            = new Dictionary<string, Dictionary<int, WorldEventModule.Model>>();

        /// <summary>
        /// Processes delayed intentions
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        protected override IEnumerator Process(IEnumerable<IndexedIntentionModule.Model> ts)
        {
            ProcessMany(ts.OrderBy(i => i.tsIntention.timestamp));
            yield return null;
        }

        /// <summary>
        /// Immediately processes an intention
        /// </summary>
        /// <param name="intention"></param>
        /// <returns>ID used for the intention</returns>
        public string Process(IntentionModule.Type intention)
        {
            var timestamp = Registry.TimestampProvider.Now;
            var payload = IntentionModule.makePayload("", intention);
            var withId = WithId.create(payload);
            var withTimestamp = WithTimestamp.create(timestamp, withId);
            var indexed = IndexedIntentionModule.create(withTimestamp);

            _intentions[withId.id] = timestamp;

            ProcessMany(new List<IndexedIntentionModule.Model> { indexed });

            return withId.id;
        }

        private void ProcessMany(IEnumerable<IndexedIntentionModule.Model> intentions)
        {
            var processResult = IntentionProcessing.processMany(
                DateTime.UtcNow.Ticks,
                FSharpOption<FSharpMap<string, string>>.None,
                _objectBusyMap,
                Registry.Stores.World.World,
                intentions
            );

            var logs = ClientLogEntryModule.createFromProcessResult(processResult);
            Registry.Processors.Logs.AddLog(logs);

            _objectBusyMap = processResult.objectBusyMap;

            Registry.Streams.WorldEvents.NextMany(processResult.events, UpdateSource.Game);

            foreach (var e in processResult.events)
            {
                if (!_eventsProduced.TryGetValue(e.resultOf, out var indexDict))
                {
                    _eventsProduced[e.resultOf] = new Dictionary<int, WorldEventModule.Model>();
                    indexDict = _eventsProduced[e.resultOf];
                }

                indexDict[e.index] = e;
            }

            foreach (var d in processResult.delayed)
            {
                Add(d);
            }
        }

        public long? BusyUntil(string objectId)
        {
            var timestamp = MapModule.TryFind(objectId, _objectBusyMap);

            if (timestamp.HasValue())
            {
                return timestamp.Value;
            }
            else
            {
                return null;
            }
        }

        public bool DidProcess(string intentionId)
        {
            return _intentions.TryGetValue(intentionId, out _);
        }
    }
}