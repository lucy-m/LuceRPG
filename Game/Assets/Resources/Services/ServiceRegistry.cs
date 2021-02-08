namespace LuceRPG.Game.Services
{
    public class ServiceRegistry
    {
        public ICommsService Comms { get; } = new CommsService();
        public WorldLoaderService WorldLoader { get; } = new WorldLoaderService();
        public IntentionService Intentions { get; } = new IntentionService();
        public ConfigLoaderService ConfigLoader { get; } = new ConfigLoaderService();
    }
}
