using System;

namespace LuceRPG.Utility
{
    public static class TimestampProvider
    {
        public static long Now => DateTime.UtcNow.Ticks;
    }
}
