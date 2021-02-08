using LuceRPG.Adapters;
using LuceRPG.Models;

namespace LuceRPG.Game.Stores
{
    public class WorldStore
    {
        public string PlayerId { get; set; }
        public string ClientId { get; set; }
        public WorldModule.Model World { get; set; }
        public InteractionStore Interactions { get; set; }
        public long LastUpdate { get; set; }

        public void Apply(WorldEventModule.Model worldEvent)
        {
            World = EventApply.apply(worldEvent, World);
        }

        public bool HasWorld()
        {
            return World != null;
        }
    }
}
