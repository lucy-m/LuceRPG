using LuceRPG.Models;
using LuceRPG.Server.Core;
using LuceRPG.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;

namespace LuceRPG.Server.Processors
{
    public sealed class IntentionProcessor
    {
        private readonly ILogger<IntentionProcessor> _logger;
        private readonly WorldEventsStorer _store;
        private readonly IntentionQueue _queue;
        private readonly ICsvLogService _logService;
        private readonly ITimestampProvider _timestampProvider;

        public IntentionProcessor(
            ILogger<IntentionProcessor> logger,
            WorldEventsStorer store,
            IntentionQueue queue,
            ICsvLogService logService,
            ITimestampProvider timestampProvider)
        {
            _store = store;
            _queue = queue;
            _logger = logger;
            _logService = logService;
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
                    _store.ServerSideData,
                    _store.ObjectBusyMap,
                    _store.WorldMap,
                    intentions
                );

                _logService.AddProcessResult(processed);

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

                var worldGenerationRequests =
                    events
                    .Where(e => e.t.IsWorldGenerateRequest)
                    .Select(e => ((WorldEventModule.Type.WorldGenerateRequest)e.t, e));

                foreach (var t in worldGenerationRequests)
                {
                    var (request, e) = t;
                    var sw = new Stopwatch();
                    sw.Start();
                    _logger.LogDebug($"Generating world {request.Item1}");
                    _store.Generate(request.Item1, e.world, request.Item2);
                    sw.Stop();
                    _logger.LogDebug($"Generated world {request.Item1} in {sw.ElapsedMilliseconds} ms");
                }
            }
        }
    }

    public sealed class IntentionProcessorService : ProcessorHostService
    {
        private readonly IntentionProcessor _intentionProcessor;
        protected override TimeSpan Interval => TimeSpan.FromMilliseconds(100);

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
