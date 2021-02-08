namespace LuceRPG.Game.Services
{
    public class ServiceRegistry
    {
        public ICommsService Comms { get; } = new CommsService();
        public WorldLoaderService WorldLoader { get; } = new WorldLoaderService();
    }
}
