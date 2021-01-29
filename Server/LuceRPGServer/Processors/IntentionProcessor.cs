using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LuceRPG.Server.Processors
{
    public sealed class IntentionProcessor : IHostedService, IDisposable
    {
        private readonly ILogger<IntentionProcessor> _logger;
        private readonly WorldEventsStorer _store;
        private readonly IntentionQueue _queue;
        private Timer? _timer;

        public IntentionProcessor(
            ILogger<IntentionProcessor> logger,
            IntentionQueue queue,
            WorldEventsStorer store
        )
        {
            _logger = logger;
            _queue = queue;
            _store = store;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Intention Processor starting");
            _timer = new Timer(ProcessIntentions, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Intention Processor stopping");
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void ProcessIntentions(object? state)
        {
            var entries = _queue.DequeueAll().ToArray();
            var entriesMap = entries.ToDictionary(e => e.Intention.value.id);

            if (entries.Length > 0)
            {
                _logger.LogDebug($"Processing {entries.Length} intentions");
                var intentions = entries.Select(e => e.Intention).ToArray();

                var processed = IntentionProcessing.processMany(
                    TimestampProvider.Now,
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
                        var myEvents = events.Where(e => e.resultOf == Intention.value.id);
                        OnProcessed(myEvents);
                    }
                }

                foreach (var i in processed.delayed)
                {
                    _logger.LogDebug($"Requeueing delayed intention {i.value.id}");
                    if (entriesMap.TryGetValue(i.value.id, out var entry))
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
}
