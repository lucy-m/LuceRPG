namespace LuceRPG.Game.Stores
{
    public class StoreRegistry
    {
        public WorldStore World { get; } = new WorldStore();
        public ConfigStore Config { get; } = new ConfigStore();
        public PerfStatsStore PerfStats { get; } = new PerfStatsStore();
    }
}
