using LuceRPG.Adapters;
using LuceRPG.Models;
using LuceRPG.Server.Core;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;

namespace LuceRPG.Server
{
    public sealed class WorldEventsStorer
    {
        private WorldEventsStoreModule.Model _store;

        private readonly ITimestampProvider _timestampProvider;
        private readonly long _cullThreshold = TimeSpan.FromMinutes(1).Ticks;

        public WorldEventsStorer(
            WorldCollectionModule.Model worldCollection,
            ITimestampProvider timestampProvider)
        {
            _store = WorldEventsStoreModule.create(worldCollection);
            _timestampProvider = timestampProvider;
        }

        public ServerSideDataModule.Model ServerSideData => _store.serverSideData;
        public FSharpMap<string, long> ObjectBusyMap => _store.objectBusyMap;
        public IEnumerable<WithId.Model<WorldModule.Payload>> AllWorlds => WorldEventsStoreModule.allWorlds(_store);
        public FSharpMap<string, WithId.Model<WorldModule.Payload>> WorldMap => _store.worldMap;

        public void Update(IntentionProcessing.ProcessManyResult result)
        {
            var newStore = WorldEventsStoreModule.addResult(result, _timestampProvider.Now, _store);
            _store = newStore;
        }

        public GetSinceResultModule.Payload GetSince(long timestamp, string clientId)
        {
            return WorldEventsStoreModule.getSince(timestamp, clientId, _store);
        }

        public FSharpList<WithId.Model<FSharpList<InteractionModule.One>>> GetInteractions(string worldId)
        {
            var fromStore = MapModule.TryFind(worldId, _store.interactionMap);
            if (fromStore.HasValue())
            {
                return fromStore.Value;
            }
            else
            {
                return ListModule.Empty<WithId.Model<FSharpList<InteractionModule.One>>>();
            }
        }

        public WithId.Model<WorldModule.Payload>? GetWorld(string worldId)
        {
            var tWorld = MapModule.TryFind(worldId, _store.worldMap);

            if (tWorld.HasValue())
            {
                return tWorld.Value;
            }
            else
            {
                return null;
            }
        }

        public string? GetWorldIdForClient(string clientId)
        {
            var tWorldId = MapModule.TryFind(clientId, ServerSideData.clientWorldMap);

            if (tWorldId.HasValue())
            {
                return tWorldId.Value;
            }
            else
            {
                return null;
            }
        }

        public void CullStore()
        {
            var timestamp = _timestampProvider.Now - _cullThreshold;
            _store = WorldEventsStoreModule.cull(timestamp, _store);
        }
    }
}
