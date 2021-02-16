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
            InteractionStore interactions,
            ITimestampProvider timestampProvider)
        {
            _store = WorldEventsStoreModule.create(worldCollection);
            _timestampProvider = timestampProvider;
            Interactions = interactions;
        }

        public ServerSideDataModule.Model ServerSideData => _store.serverSideData;
        public FSharpMap<string, long> ObjectBusyMap => _store.objectBusyMap;
        public InteractionStore Interactions { get; }
        public IEnumerable<WithId.Model<WorldModule.Payload>> AllWorlds => WorldEventsStoreModule.allWorlds(_store);

        public void Update(IntentionProcessing.ProcessResult result)
        {
            var newStore = WorldEventsStoreModule.addResult(result, _timestampProvider.Now, _store);
            _store = newStore;
        }

        public GetSinceResultModule.Payload GetSince(long timestamp, string clientId)
        {
            var worldId = GetWorldIdForClient(clientId);

            if (worldId == null)
            {
                var failure = $"No world associated with client id";
                return GetSinceResultModule.Payload.NewFailure(failure);
            }
            else
            {
                return WorldEventsStoreModule.getSince(timestamp, worldId, _store);
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
