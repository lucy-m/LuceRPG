using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LuceRPG.Server.Processors
{
    public sealed class StaleClientProcessor : IHostedService, IDisposable
    {
        private readonly ILogger<StaleClientProcessor> _logger;
        private readonly LastPingStorer _pingStore;
        private readonly IntentionQueue _queue;
        private Timer? _timer;

        public StaleClientProcessor(
            ILogger<StaleClientProcessor> logger,
            IntentionQueue queue,
            LastPingStorer pingStore
        )
        {
            _logger = logger;
            _queue = queue;
            _pingStore = pingStore;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stale Client Processor starting");
            _timer = new Timer(ProcessStaleClients, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellation)
        {
            _logger.LogInformation("Stale Client Processor stopping");
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void ProcessStaleClients(object? state)
        {
            var leaveIntentions = _pingStore.Cull().ToArray();

            if (leaveIntentions.Any())
            {
                _logger.LogDebug($"Removing {leaveIntentions.Length} stale clients");
                foreach (var i in leaveIntentions)
                {
                    _queue.Enqueue(i);
                }
            }
        }
    }
}
