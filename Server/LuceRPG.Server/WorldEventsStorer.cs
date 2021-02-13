using LuceRPG.Adapters;
using LuceRPG.Models;
using LuceRPG.Server.Core;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using System;

namespace LuceRPG.Server
{
    public sealed class WorldEventsStorer
    {
        private WorldEventsStoreModule.Model _store;

        private readonly ITimestampProvider _timestampProvider;
        private readonly long _cullThreshold = TimeSpan.FromMinutes(1).Ticks;

        public WorldEventsStorer(
            WithId.Model<WorldModule.Payload> initialWorld,
            InteractionStore interactions,
            ITimestampProvider timestampProvider)
        {
            _store = WorldEventsStoreModule.create(initialWorld);
            _timestampProvider = timestampProvider;
            Interactions = interactions;
        }

        public WithId.Model<WorldModule.Payload> CurrentWorld => _store.world;
        public ServerSideDataModule.Model ServerSideData => _store.serverSideData;
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

        public void CullStore()
        {
            var timestamp = _timestampProvider.Now - _cullThreshold;
            _store = WorldEventsStoreModule.cull(timestamp, _store);
        }
    }
}
