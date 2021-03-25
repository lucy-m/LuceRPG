using LuceRPG.Models;
using LuceRPG.Server.Storer;
using LuceRPG.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LuceRPG.Server.Processors
{
    public sealed class BehaviourProcessor
    {
        private readonly ITimestampProvider _timestampProvider;
        private readonly WorldEventsStorer _worldEventsStorer;
        private readonly IntentionQueue _intentionQueue;
        private readonly BehaviourMapStorer _mapStorer;
        private readonly ICsvLogService _logService;

        public BehaviourProcessor(
            ITimestampProvider timestampProvider,
            WorldEventsStorer worldEventsStorer,
            BehaviourMapStorer mapStorer,
            IntentionQueue intentionQueue,
            ICsvLogService logService)
        {
            _timestampProvider = timestampProvider;
            _worldEventsStorer = worldEventsStorer;
            _mapStorer = mapStorer;
            _intentionQueue = intentionQueue;
            _logService = logService;
        }

        // Worth noting that this processor should not run multiple times between
        //   intention processing ticks. This will result in duplicate intentions
        //   being dispatched and may be a source of bugs.
        // In this case, can track which behaviours have an intention that is awaiting
        //   processing, and ignoring those behaviours.
        public void Process()
        {
            var now = _timestampProvider.Now;
            var busyMap = _worldEventsStorer.ObjectBusyMap;
            var serverId = _worldEventsStorer.ServerSideData.serverId;

            var results = _mapStorer.Maps.Select(kvp =>
            {
                var worldId = kvp.Key;
                var behaviour = kvp.Value;

                var updateResult = BehaviourMapModule.update(now, busyMap, behaviour);
                var indexedIntentions = updateResult.intentions.Select(t =>
                {
                    var payload = IntentionModule.makePayload(serverId, t);
                    var withId = WithId.create(payload);
                    var withTimestamp = WithTimestamp.create(now, withId);
                    var indexed = IndexedIntentionModule.create.Invoke(worldId).Invoke(withTimestamp);

                    return indexed;
                });

                _logService.AddBehaviourUpdateResult(updateResult);

                var updatedBehaviour = KeyValuePair.Create(worldId, updateResult.model);

                return (Behaviour: updatedBehaviour, Intentions: indexedIntentions);
            }).ToArray();

            var intentions = results.SelectMany(t => t.Intentions).ToArray();
            var behaviours = results.Select(t => t.Behaviour).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            _intentionQueue.EnqueueAll(intentions);

            foreach (var b in behaviours)
            {
                _mapStorer.Update(b.Key, b.Value);
            }
        }
    }

    public sealed class BehaviourProcessorService : ProcessorHostService
    {
        private readonly BehaviourProcessor _behaviourProcessor;
        protected override TimeSpan Interval => TimeSpan.FromMilliseconds(200);

        public BehaviourProcessorService(
            ILogger<ProcessorHostService> logger, BehaviourProcessor behaviourProcessor)
            : base(logger)
        {
            _behaviourProcessor = behaviourProcessor;
        }

        protected override void DoProcess()
        {
            _behaviourProcessor.Process();
        }
    }
}
