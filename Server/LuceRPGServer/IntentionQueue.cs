using LuceRPG.Models;
using System;
using System.Collections.Generic;

namespace LuceRPG.Server
{
    public class IntentionQueue
    {
        private readonly Queue<WithId.Model<IntentionModule.Payload>> _queue;

        public IntentionQueue()
        {
            _queue = new Queue<WithId.Model<IntentionModule.Payload>>();
        }

        public void Enqueue(WithId.Model<IntentionModule.Payload> intention)
        {
            lock (_queue)
            {
                _queue.Enqueue(intention);
            }
        }

        public IEnumerable<WithId.Model<IntentionModule.Payload>> DequeueAll()
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
