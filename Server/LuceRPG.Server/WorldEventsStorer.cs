using LuceRPG.Adapters;
using LuceRPG.Models;
using LuceRPG.Server.Core;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;

namespace LuceRPG.Server
{
    public sealed class WorldEventsStorer
    {
        private WorldEventsStoreModule.Model _store;

        private readonly ITimestampProvider _timestampProvider;

        public WorldEventsStorer(
            WorldModule.Model initialWorld,
            InteractionStore interactions,
            ITimestampProvider timestampProvider)
        {
            _store = WorldEventsStoreModule.create(initialWorld);
            _timestampProvider = timestampProvider;
            Interactions = interactions;
        }

        public WorldModule.Model CurrentWorld => _store.world;
        public FSharpMap<string, string> ObjectClientMap => _store.objectClientMap;
        public FSharpMap<string, long> ObjectBusyMap => _store.objectBusyMap;
        public InteractionStore Interactions { get; }

        public void Update(IntentionProcessing.ProcessResult result)
        {
            var newStore = WorldEventsStoreModule.addResult(result, _timestampProvider.Now, _store);
            _store = newStore;
        }

        public GetSinceResultModule.Payload GetSince(long timestamp)
        {
            return WorldEventsStoreModule.getSince(timestamp, _store);
        }
    }
}
