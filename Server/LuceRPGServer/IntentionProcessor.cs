using LuceRPG.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Collections;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LuceRPG.Server
{
    public sealed class IntentionProcessor : IHostedService, IDisposable
    {
        private readonly ILogger<IntentionProcessor> _logger;
        private readonly WorldEventsStorer _store;
        private readonly IntentionQueue _queue;
        private Timer? _timer;

        private FSharpMap<string, string> _objectClientMap;

        public IntentionProcessor(
            ILogger<IntentionProcessor> logger,
            IntentionQueue queue,
            WorldEventsStorer store
        )
        {
            _logger = logger;
            _queue = queue;
            _store = store;

            // So far the only way for ownership to be established is
            //   through intentions
            // A freshly loaded world will never have any ownership
            _objectClientMap = new FSharpMap<string, string>(Array.Empty<Tuple<string, string>>());
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Intention processor starting");
            _timer = new Timer(ProcessIntentions, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Intention processor stopping");
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void ProcessIntentions(object? state)
        {
            var entries = _queue.DequeueAll().ToArray();

            if (entries.Length > 0)
            {
                _logger.LogDebug($"Processing {entries.Length} intentions");
                var intentions = entries.Select(e => e.Intention).ToArray();
                var processed = IntentionProcessing.processMany(_objectClientMap, _store.CurrentWorld, intentions);

                var events = processed.events.ToArray();
                foreach (var (Intention, OnProcessed) in entries)
                {
                    if (OnProcessed != null)
                    {
                        var myEvents = events.Where(e => e.resultOf == Intention.id);
                        OnProcessed(myEvents);
                    }
                }

                _store.Update(processed);
                _objectClientMap = processed.objectClientMap;
            }
        }
    }
}
