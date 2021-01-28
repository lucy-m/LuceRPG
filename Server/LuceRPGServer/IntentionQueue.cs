using LuceRPG.Models;
using System;
using System.Collections.Generic;

namespace LuceRPG.Server
{
    public class IntentionQueue
    {
        private readonly Queue<(
            WithTimestamp.Model<WithId.Model<IntentionModule.Payload>> Intention,
            Action<IEnumerable<WorldEventModule.Model>>? OnProcessed
        )> _queue;

        public IntentionQueue()
        {
            _queue = new Queue<(
                WithTimestamp.Model<WithId.Model<IntentionModule.Payload>> Intention,
                Action<IEnumerable<WorldEventModule.Model>>? OnProcessed
            )>();
        }

        public void Enqueue(
            WithTimestamp.Model<WithId.Model<IntentionModule.Payload>> intention,
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
                _queue.Enqueue((Intention: timestamped, OnProcessed: onProcessed));
            }
        }

        public IEnumerable<(
            WithTimestamp.Model<WithId.Model<IntentionModule.Payload>> Intention,
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
