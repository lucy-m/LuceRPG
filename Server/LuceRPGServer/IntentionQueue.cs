using LuceRPG.Models;
using System;
using System.Collections.Generic;

namespace LuceRPG.Server
{
    public class IntentionQueue
    {
        private readonly Queue<(
            WithId.Model<IntentionModule.Payload> Intention,
            Action<IEnumerable<WorldEventModule.Model>>? OnProcessed
        )> _queue;

        public IntentionQueue()
        {
            _queue = new Queue<(
                WithId.Model<IntentionModule.Payload> Intention,
                Action<IEnumerable<WorldEventModule.Model>>? OnProcessed
            )>();
        }

        public void Enqueue(WithId.Model<IntentionModule.Payload> intention)
        {
            lock (_queue)
            {
                _queue.Enqueue((Intention: intention, OnProcessed: null));
            }
        }

        public void Enqueue(
            WithId.Model<IntentionModule.Payload> intention,
            Action<IEnumerable<WorldEventModule.Model>> onProcessed
        )
        {
            lock (_queue)
            {
                _queue.Enqueue((Intention: intention, OnProcessed: onProcessed));
            }
        }

        public IEnumerable<(
            WithId.Model<IntentionModule.Payload> Intention,
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
