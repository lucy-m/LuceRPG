using LuceRPG.Game.Services;
using LuceRPG.Game.Stores;
using LuceRPG.Utility;

namespace LuceRPG.Game
{
    public static class Registry
    {
        public static StoreRegistry Stores { get; private set; } = new StoreRegistry();
        public static ServiceRegistry Services { get; private set; } = new ServiceRegistry();
        public static ITimestampProvider TimestampProvider { get; set; } = new TimestampProvider();

        public static void Reset()
        {
            Stores = new StoreRegistry();
            Services = new ServiceRegistry();
            TimestampProvider = new TimestampProvider();
        }
    }
}
