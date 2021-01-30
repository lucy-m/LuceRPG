using System;

namespace LuceRPG.Utility
{
    public interface ITimestampProvider
    {
        long Now { get; }
    }

    public class TimestampProvider : ITimestampProvider
    {
        public long Now => DateTime.UtcNow.Ticks;
    }

    public class TestTimestampProvider : ITimestampProvider
    {
        public long Now { get; set; }
    }
}
