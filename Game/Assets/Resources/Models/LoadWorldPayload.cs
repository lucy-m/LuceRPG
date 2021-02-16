using LuceRPG.Adapters;
using LuceRPG.Models;

namespace LuceRPG.Game.Models
{
    public class LoadWorldPayload
    {
        public string ClientId { get; }
        public string PlayerId { get; }
        public WithTimestamp.Model<WithId.Model<WorldModule.Payload>> TsWorld { get; }
        public InteractionStore Interactions { get; }

        public LoadWorldPayload(
            string clientId,
            string playerId,
            WithTimestamp.Model<WorldModule.Model> tsWorld,
            InteractionStore interactions)
        {
            ClientId = clientId;
            PlayerId = playerId;
            TsWorld = tsWorld;
            Interactions = interactions;
        }
    }
}
