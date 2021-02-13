using LuceRPG.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace LuceRPG.Server.Processors
{
    public class StaleClientProcessor
    {
        private readonly ILogger<StaleClientProcessor> _logger;
        private readonly LastPingStorer _pingStore;
        private readonly IntentionQueue _queue;
        private readonly ITimestampProvider _timestampProvider;

        public StaleClientProcessor(
            ILogger<StaleClientProcessor> logger,
            IntentionQueue queue,
            LastPingStorer pingStore,
            ITimestampProvider timestampProvider)
        {
            _logger = logger;
            _queue = queue;
            _pingStore = pingStore;
            _timestampProvider = timestampProvider;
        }

        public void ProcessStaleClients(int staleThresholdSec)
        {
            var staleThreshold = _timestampProvider.Now - TimeSpan.FromSeconds(staleThresholdSec).Ticks;

            var leaveIntentions = _pingStore.Cull(staleThreshold).ToArray();

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

    public sealed class StaleClientProcessorService : ProcessorHostService
    {
        private readonly StaleClientProcessor _staleClientProcessor;
        protected override TimeSpan Interval => TimeSpan.FromSeconds(15);

        public StaleClientProcessorService(ILogger<ProcessorHostService> logger, StaleClientProcessor staleClientProcessor)
            : base(logger)
        {
            _staleClientProcessor = staleClientProcessor;
        }

        protected override void DoProcess()
        {
            _staleClientProcessor.ProcessStaleClients(10);
        }
    }
}
