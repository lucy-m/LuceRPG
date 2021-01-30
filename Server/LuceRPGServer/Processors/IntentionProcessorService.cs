﻿using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace LuceRPG.Server.Processors
{
    public sealed class IntentionProcessor
    {
        private readonly ILogger<IntentionProcessor> _logger;
        private readonly WorldEventsStorer _store;
        private readonly IntentionQueue _queue;
        private readonly ITimestampProvider _timestampProvider;

        public IntentionProcessor(
            ILogger<IntentionProcessor> logger,
            WorldEventsStorer store,
            IntentionQueue queue,
            ITimestampProvider timestampProvider)
        {
            _store = store;
            _queue = queue;
            _logger = logger;
            _timestampProvider = timestampProvider;
        }

        public void Process()
        {
            var timestamp = _timestampProvider.Now;
            var entries = _queue.DequeueAll().ToArray();
            var entriesMap = entries.ToDictionary(e => e.Intention.tsIntention.value.id);

            if (entries.Length > 0)
            {
                _logger.LogDebug($"Processing {entries.Length} intentions");
                var intentions = entries.Select(e => e.Intention).ToArray();

                var processed = IntentionProcessing.processMany(
                    timestamp,
                    _store.ObjectClientMap,
                    _store.ObjectBusyMap,
                    _store.CurrentWorld,
                    intentions
                );

                var events = processed.events.ToArray();
                foreach (var (Intention, OnProcessed) in entries)
                {
                    if (OnProcessed != null)
                    {
                        var myEvents = events.Where(e => e.resultOf == Intention.tsIntention.value.id);
                        OnProcessed(myEvents);
                    }
                }

                foreach (var i in processed.delayed)
                {
                    _logger.LogDebug($"Requeueing delayed intention {i.tsIntention.value.id} at index {i.index}");
                    if (entriesMap.TryGetValue(i.tsIntention.value.id, out var entry))
                    {
                        _queue.Enqueue(i, entry.OnProcessed);
                    }
                    else
                    {
                        _queue.Enqueue(i);
                    }
                }

                _store.Update(processed);
            }
        }
    }

    public sealed class IntentionProcessorService : ProcessorHostService
    {
        private readonly IntentionProcessor _intentionProcessor;

        public IntentionProcessorService(
            ILogger<ProcessorHostService> logger, IntentionProcessor intentionProcessor)
            : base(logger)
        {
            _intentionProcessor = intentionProcessor;
        }

        protected override void DoProcess()
        {
            _intentionProcessor.Process();
        }
    }
}
