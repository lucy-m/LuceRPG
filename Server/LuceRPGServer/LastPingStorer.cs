using LuceRPG.Models;
using LuceRPG.Server.Core;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;

namespace LuceRPG.Server
{
    public class LastPingStorer
    {
        private FSharpMap<string, long> _store;

        public LastPingStorer()
        {
            _store = new FSharpMap<string, long>(Array.Empty<Tuple<string, long>>());
        }

        public void Update(string clientId, long timestamp)
        {
            _store = _store.Add(clientId, timestamp);
        }

        public IEnumerable<WithId.Model<IntentionModule.Payload>> Cull()
        {
            var staleThreshold = TimestampProvider.Now - TimeSpan.FromSeconds(10).Ticks;

            var culled = LastPingStore.cull(staleThreshold, _store);
            _store = culled.updated;

            return culled.intentions;
        }
    }
}
