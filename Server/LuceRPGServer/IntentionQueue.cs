using LuceRPG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LuceRPG.Server
{
    public class IntentionQueue
    {
        private readonly Queue<WithGuid.Model<IntentionModule.Payload>> _queue;

        public IntentionQueue()
        {
            _queue = new Queue<WithGuid.Model<IntentionModule.Payload>>();
        }

        public void Enqueue(WithGuid.Model<IntentionModule.Payload> intention)
        {
            lock (_queue)
            {
                _queue.Enqueue(intention);
            }
        }

        public IEnumerable<WithGuid.Model<IntentionModule.Payload>> DequeueAll()
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
