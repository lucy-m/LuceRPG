using LuceRPG.Models;
using LuceRPG.Server.Core;
using Microsoft.FSharp.Collections;

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
        public FSharpMap<string, string> ObjectClientMap => _store.objectClientMap;
        public FSharpMap<string, long> ObjectBusyMap => _store.objectBusyMap;

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
