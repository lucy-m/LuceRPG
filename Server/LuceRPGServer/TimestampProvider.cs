using System;

namespace LuceRPG.Server
{
    public static class TimestampProvider
    {
        public static long Now => DateTime.UtcNow.Ticks;
    }
}
