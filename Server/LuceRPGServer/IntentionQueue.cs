using LuceRPG.Models;
using LuceRPG.Utility;
using System;
using System.Collections.Generic;

namespace LuceRPG.Server
{
    public class IntentionQueue
    {
        private readonly Queue<(
            IntentionProcessing.IndexedIntentionModule.Model Intention,
            Action<IEnumerable<WorldEventModule.Model>>? OnProcessed
        )> _queue;

        public IntentionQueue()
        {
            _queue = new Queue<(
                IntentionProcessing.IndexedIntentionModule.Model Intention,
                Action<IEnumerable<WorldEventModule.Model>>? OnProcessed
            )>();
        }

        public void Enqueue(
            IntentionProcessing.IndexedIntentionModule.Model intention,
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
            Action<IEnumerable<WorldEventModule.Model>>? onProcessed = null
        )
        {
            lock (_queue)
            {
                var timestamped = WithTimestamp.create(TimestampProvider.Now, intention);
                var indexed = IntentionProcessing.IndexedIntentionModule.create(timestamped);

                _queue.Enqueue((Intention: indexed, OnProcessed: onProcessed));
            }
        }

        public IEnumerable<(
            IntentionProcessing.IndexedIntentionModule.Model Intention,
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
