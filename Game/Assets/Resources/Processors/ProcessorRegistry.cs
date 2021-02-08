using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuceRPG.Game.Processors
{
    public class ProcessorRegistry
    {
        public LogProcessor Logs { get; } = new LogProcessor();
        public OptimisticIntentionProcessor Intentions { get; } = new OptimisticIntentionProcessor();
    }
}
