using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LuceRPG.Server.Processors
{
    public abstract class ProcessorHostService : IHostedService, IDisposable
    {
        private readonly ILogger<ProcessorHostService> _logger;
        private Timer? _timer;

        public ProcessorHostService(ILogger<ProcessorHostService> logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processor starting");
            _timer = new Timer(ProcessIntentions, null, TimeSpan.Zero, Interval);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processor stopping");
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void ProcessIntentions(object? state)
        {
            DoProcess();
        }

        protected abstract void DoProcess();

        protected abstract TimeSpan Interval { get; }
    }
}
