using LuceRPG.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private Timer _timer;

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
            _logger.LogInformation("Intention processor starting");
            _timer = new Timer(ProcessIntentions, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Intention processor stopping");
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void ProcessIntentions(object state)
        {
            var intentions = _queue.DequeueAll().ToArray();

            if (intentions.Length > 0)
            {
                _logger.LogDebug($"Processing {intentions.Length} intentions");
                var processed = IntentionProcessing.processMany(intentions, _store.CurrentWorld);
                _store.Update(processed);
            }
        }
    }
}
