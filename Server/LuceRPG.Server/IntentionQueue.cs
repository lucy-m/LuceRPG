using LuceRPG.Models;
using LuceRPG.Utility;
using System;
using System.Collections.Generic;

namespace LuceRPG.Server
{
    public class IntentionQueue
    {
        private readonly ITimestampProvider _timestampProvider;
        private readonly ICsvLogService logService;

        private readonly Queue<(
            IndexedIntentionModule.Model Intention,
            Action<IEnumerable<WorldEventModule.Model>>? OnProcessed
        )> _queue;

        public Queue<(
            IndexedIntentionModule.Model Intention,
            Action<IEnumerable<WorldEventModule.Model>>? OnProcessed
        )> Queue => _queue;

        public IntentionQueue(
            ITimestampProvider timestampProvider,
            ICsvLogService logService)
        {
            _queue = new Queue<(
                IndexedIntentionModule.Model Intention,
                Action<IEnumerable<WorldEventModule.Model>>? OnProcessed
            )>();
            _timestampProvider = timestampProvider;
            this.logService = logService;
        }

        public void Enqueue(
            IndexedIntentionModule.Model intention,
            Action<IEnumerable<WorldEventModule.Model>>? onProcessed = null
        )
        {
            lock (_queue)
            {
                logService.AddIntentionQueue(intention);
                _queue.Enqueue((Intention: intention, OnProcessed: onProcessed));
            }
        }

        public void Enqueue(
            WithId.Model<IntentionModule.Payload> intention,
            string worldId,
            Action<IEnumerable<WorldEventModule.Model>>? onProcessed = null
        )
        {
            lock (_queue)
            {
                var timestamped = WithTimestamp.create(_timestampProvider.Now, intention);
                var indexed = IndexedIntentionModule.create.Invoke(worldId).Invoke(timestamped);

                logService.AddIntentionQueue(indexed);

                _queue.Enqueue((Intention: indexed, OnProcessed: onProcessed));
            }
        }

        public void EnqueueAll(
            IEnumerable<IndexedIntentionModule.Model> intentions
        )
        {
            lock (_queue)
            {
                foreach (var i in intentions)
                {
                    logService.AddIntentionQueue(i);
                    _queue.Enqueue((Intention: i, OnProcessed: null));
                }
            }
        }

        public IEnumerable<(
            IndexedIntentionModule.Model Intention,
            Action<IEnumerable<WorldEventModule.Model>>? OnProcessed
        )> DequeueAll()
        {
            lock (_queue)
            {
                while (_queue.TryDequeue(out var intention))
                {
                    yield return intention;
                }
            }
        }
    }
}
