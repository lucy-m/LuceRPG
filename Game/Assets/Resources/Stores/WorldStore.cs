using LuceRPG.Adapters;
using LuceRPG.Game.Models;
using LuceRPG.Models;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace LuceRPG.Game.Stores
{
    public class WorldStore
    {
        public string PlayerId { get; private set; }
        public string ClientId { get; private set; }
        public WithId.Model<WorldModule.Payload> IdWorld { get; set; }
        public InteractionStore Interactions { get; set; }
        public long LastUpdate { get; set; }

        public string WorldId => IdWorld.id;

        public WorldModule.Payload World
        {
            get => IdWorld?.value;
            set =>
                IdWorld = WithId.useId(WorldId, value);
        }

        public void Apply(WorldEventModule.Model worldEvent)
        {
            IdWorld = EventApply.apply(worldEvent, IdWorld);
        }

        public void LoadFrom(LoadWorldPayload payload)
        {
            PlayerId = payload.PlayerId;
            ClientId = payload.ClientId;
            IdWorld = payload.IdWorld.value;
            Interactions = payload.Interactions;
            LastUpdate = payload.IdWorld.timestamp;
        }

        public bool HasWorld()
        {
            return World != null;
        }

        public FSharpOption<WithId.Model<WorldObjectModule.Payload>> GetObject(string id)
        {
            if (World == null)
            {
                return FSharpOption<WithId.Model<WorldObjectModule.Payload>>.None;
            }
            else
            {
                return MapModule.TryFind(id, World.objects);
            }
        }
    }
}
