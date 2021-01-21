using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LuceRPG.Server
{
    public class IntentionProcessor : IHostedService, IDisposable
    {
        private readonly ILogger<IntentionProcessor> _logger;
        private readonly IntentionQueue _queue;
        private Timer _timer;

        public IntentionProcessor(ILogger<IntentionProcessor> logger, IntentionQueue queue)
        {
            _logger = logger;
            _queue = queue;
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
            _logger.LogDebug("Intention processor doing work");
            var intentions = _queue.DequeueAll().ToArray();

            _logger.LogDebug($"Got {intentions.Length} intentions");
        }
    }
}
