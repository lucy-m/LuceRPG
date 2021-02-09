using LuceRPG.Game.Processors;
using LuceRPG.Game.Providers;
using LuceRPG.Game.Services;
using LuceRPG.Game.Stores;
using LuceRPG.Game.Streams;
using LuceRPG.Utility;

namespace LuceRPG.Game
{
    public static class Registry
    {
        public static StoreRegistry Stores { get; private set; } = new StoreRegistry();
        public static ServiceRegistry Services { get; private set; } = new ServiceRegistry();
        public static ProcessorRegistry Processors { get; private set; } = new ProcessorRegistry();
        public static StreamRegistry Streams { get; private set; } = new StreamRegistry();
        public static ProviderRegistry Providers { get; private set; } = new ProviderRegistry();

        public static void Reset()
        {
            Stores = new StoreRegistry();
            Services = new ServiceRegistry();
            Processors = new ProcessorRegistry();
            Streams = new StreamRegistry();
            Providers = new ProviderRegistry();
        }
    }
}
