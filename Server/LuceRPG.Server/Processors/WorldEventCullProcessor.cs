using Microsoft.Extensions.Logging;
using System;

namespace LuceRPG.Server.Processors
{
    public sealed class WorldEventCullService : ProcessorHostService
    {
        public WorldEventsStorer _storer;
        protected override TimeSpan Interval => TimeSpan.FromSeconds(15);

        public WorldEventCullService(
            ILogger<ProcessorHostService> logger,
            WorldEventsStorer storer)
            : base(logger)
        {
            _storer = storer;
        }

        protected override void DoProcess()
        {
            _storer.CullStore();
        }
    }
}
