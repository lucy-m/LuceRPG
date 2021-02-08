using LuceRPG.Game.Models;
using LuceRPG.Models;
using LuceRPG.Utility;
using System;
using System.Collections.Generic;

namespace LuceRPG.Game.Streams
{
    public class WorldEventStream
    {
        private readonly List<Action<WithId.Model<WorldObjectModule.Payload>>> _onAddHandlers
            = new List<Action<WithId.Model<WorldObjectModule.Payload>>>();

        private readonly List<Action<string, WorldEventModule.Model>> _onUcEventHandlers
            = new List<Action<string, WorldEventModule.Model>>();

        public void NextMany(IEnumerable<WorldEventModule.Model> worldEvents, UpdateSource source)
        {
            foreach (var we in worldEvents)
            {
                Next(we, source);
            }
        }

        public void Next(WorldEventModule.Model worldEvent, UpdateSource source)
        {
            if (source == UpdateSource.Server &&
                Registry.Processors.Intentions.DidProcess(worldEvent.resultOf))
            {
                var log = ClientLogEntryModule.Payload.NewUpdateIgnored(worldEvent);
                Registry.Processors.Logs.AddLog(log);
            }
            else
            {
                Registry.Stores.World.Apply(worldEvent);

                var tObjectId = WorldEventModule.getObjectId(worldEvent.t);
                if (tObjectId.HasValue())
                {
                    var objectId = tObjectId.Value;

                    if (worldEvent.t.IsObjectAdded)
                    {
                        var objectAdded = ((WorldEventModule.Type.ObjectAdded)worldEvent.t).Item;
                        foreach (var handler in _onAddHandlers)
                        {
                            handler(objectAdded);
                        }
                    }
                    else
                    {
                        foreach (var handler in _onUcEventHandlers)
                        {
                            handler(objectId, worldEvent);
                        }
                    }
                }
            }
        }

        public void RegisterOnAdd(Action<WithId.Model<WorldObjectModule.Payload>> handler)
        {
            _onAddHandlers.Add(handler);
        }

        public void RegisterOnUcEvent(Action<string, WorldEventModule.Model> handler)
        {
            _onUcEventHandlers.Add(handler);
        }
    }
}
