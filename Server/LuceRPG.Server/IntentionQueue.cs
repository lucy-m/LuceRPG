using LuceRPG.Models;
using LuceRPG.Utility;
using System;
using System.Collections.Generic;

namespace LuceRPG.Server
{
    public class IntentionQueue
    {
        private readonly ITimestampProvider _timestampProvider;

        private readonly Queue<(
            IndexedIntentionModule.Model Intention,
            Action<IEnumerable<WorldEventModule.Model>>? OnProcessed
        )> _queue;

        public Queue<(
            IndexedIntentionModule.Model Intention,
            Action<IEnumerable<WorldEventModule.Model>>? OnProcessed
        )> Queue => _queue;

        public IntentionQueue(ITimestampProvider timestampProvider)
        {
            _queue = new Queue<(
                IndexedIntentionModule.Model Intention,
                Action<IEnumerable<WorldEventModule.Model>>? OnProcessed
            )>();
            _timestampProvider = timestampProvider;
        }

        public void Enqueue(
            IndexedIntentionModule.Model intention,
            Action<IEnumerable<WorldEventModule.Model>>? onProcessed = null
        )
        {
            lock (_queue)
            {
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

                _queue.Enqueue((Intention: indexed, OnProcessed: onProcessed));
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
