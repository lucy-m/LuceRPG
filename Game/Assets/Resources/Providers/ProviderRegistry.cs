using LuceRPG.Utility;

namespace LuceRPG.Game.Providers
{
    public class ProviderRegistry
    {
        public IInputProvider Input { get; set; } = new InputProvider();
        public ITimestampProvider Timestamp { get; set; } = new TimestampProvider();
    }
}
