using LuceRPG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LuceRPG.Server
{
    public class IntentionQueue
    {
        private readonly Queue<IntentionModule.Model> _queue;

        public IntentionQueue()
        {
            _queue = new Queue<IntentionModule.Model>();
        }

        public void Enqueue(IntentionModule.Model intention)
        {
            lock (_queue)
            {
                _queue.Enqueue(intention);
            }
        }

        public IEnumerable<IntentionModule.Model> DequeueAll()
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
