using LuceRPG.Models;
using LuceRPG.Server.Core;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LuceRPG.Server.Storer
{
    public sealed class BehaviourMapStorer
    {
        public Dictionary<string, FSharpMap<string, WithId.Model<BehaviourModule.Model>>> Maps { get; }

        public BehaviourMapStorer(WorldCollectionModule.Model worldCollection)
        {
            Maps = new Dictionary<string, FSharpMap<string, WithId.Model<BehaviourModule.Model>>>();

            foreach (var w in worldCollection.allWorlds)
            {
                var worldId = w.Item1.id;
                var behaviours = w.Item3;

                Maps[worldId] = behaviours;
            }
        }

        public void Update(string worldId, FSharpMap<string, WithId.Model<BehaviourModule.Model>> map)
        {
            Maps[worldId] = map;
        }
    }
}

