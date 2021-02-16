using LuceRPG.Adapters;
using LuceRPG.Models;

namespace LuceRPG.Game.Models
{
    public class LoadWorldPayload
    {
        public string ClientId { get; }
        public string PlayerId { get; }
        public WithTimestamp.Model<WithId.Model<WorldModule.Payload>> IdWorld { get; }
        public InteractionStore Interactions { get; }

        public LoadWorldPayload(
            string clientId,
            string playerId,
            WithTimestamp.Model<WithId.Model<WorldModule.Payload>> idWorld,
            InteractionStore interactions)
        {
            ClientId = clientId;
            PlayerId = playerId;
            IdWorld = idWorld;
            Interactions = interactions;
        }
    }
}
