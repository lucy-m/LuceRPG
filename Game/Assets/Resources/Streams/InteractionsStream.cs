using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LuceRPG.Game.Streams
{
    public class InteractionsStream
    {
        private readonly List<Action<string, Vector3, WithId.Model<FSharpList<InteractionModule.One>>>> _onInteractHandlers
            = new List<Action<string, Vector3, WithId.Model<FSharpList<InteractionModule.One>>>>();

        public void Next(string objId, Vector3 position)
        {
            var interaction = WorldModule.getInteraction(
                objId,
                Registry.Stores.World.Interactions.Value,
                Registry.Stores.World.World
            );

            if (interaction.HasValue())
            {
                foreach (var handler in _onInteractHandlers)
                {
                    handler(objId, position, interaction.Value);
                }
            }
        }

        public void RegisterOnInteract(Action<string, Vector3, WithId.Model<FSharpList<InteractionModule.One>>> handler)
        {
            _onInteractHandlers.Add(handler);
        }
    }
}
