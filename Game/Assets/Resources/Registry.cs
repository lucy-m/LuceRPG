using LuceRPG.Game.Stores;

namespace LuceRPG.Game
{
    public static class Registry
    {
        public static StoreRegistry Stores { get; private set; } = new StoreRegistry();

        public static void Reset()
        {
            Stores = new StoreRegistry();
        }
    }
}
