using LuceRPG.Models;
using LuceRPG.Server.Core;

namespace LuceRPG.Server
{
    public sealed class WorldEventsStorer
    {
        private WorldEventsStoreModule.Model _store;

        public WorldEventsStorer(WorldModule.Model initialWorld)
        {
            _store = WorldEventsStoreModule.create(initialWorld);
        }

        public WorldModule.Model CurrentWorld => _store.world;

        public void Update(IntentionProcessing.ProcessResult result)
        {
            var newStore = WorldEventsStoreModule.addResult(result, TimestampProvider.Now, _store);
            _store = newStore;
        }

        public GetSinceResultModule.Payload GetSince(long timestamp)
        {
            return WorldEventsStoreModule.getSince(timestamp, _store);
        }
    }
}
