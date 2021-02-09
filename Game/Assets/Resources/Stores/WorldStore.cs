using LuceRPG.Adapters;
using LuceRPG.Game.Models;
using LuceRPG.Models;

namespace LuceRPG.Game.Stores
{
    public class WorldStore
    {
        public string PlayerId { get; private set; }
        public string ClientId { get; private set; }
        public WorldModule.Model World { get; set; }
        public InteractionStore Interactions { get; private set; }
        public long LastUpdate { get; set; }

        public void Apply(WorldEventModule.Model worldEvent)
        {
            World = EventApply.apply(worldEvent, World);
        }

        public void LoadFrom(LoadWorldPayload payload)
        {
            PlayerId = payload.PlayerId;
            ClientId = payload.ClientId;
            World = payload.TsWorld.value;
            Interactions = payload.Interactions;
            LastUpdate = payload.TsWorld.timestamp;
        }

        public bool HasWorld()
        {
            return World != null;
        }
    }
}
