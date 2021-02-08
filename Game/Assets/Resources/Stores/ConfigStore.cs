using LuceRPG.Game.Models;

namespace LuceRPG.Game.Stores
{
    public class ConfigStore
    {
        public string Path { get; } = "config.json";
        public Config Config { get; set; }
    }
}
