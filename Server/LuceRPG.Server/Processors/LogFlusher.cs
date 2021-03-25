using Microsoft.Extensions.Logging;
using System;

namespace LuceRPG.Server.Processors
{
    public sealed class LogFlusher : ProcessorHostService
    {
        private readonly ICsvLogService _logService;
        protected override TimeSpan Interval => TimeSpan.FromSeconds(1);

        public LogFlusher(
            ILogger<ProcessorHostService> logger,
            ICsvLogService logService) : base(logger)
        {
            _logService = logService;
        }

        protected override void DoProcess()
        {
            _logService.Flush();
        }
    }
}
