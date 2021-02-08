namespace LuceRPG.Game.Services
{
    public class ServiceRegistry
    {
        public ICommsService Comms { get; } = new CommsService();
        public WorldLoader WorldLoader { get; } = new WorldLoaderService();
    }
}
