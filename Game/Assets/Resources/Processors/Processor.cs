using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LuceRPG.Game.Processors
{
    public abstract class Processor<T>
    {
        public abstract float PollPeriod { get; }

        private List<T> _items = new List<T>();

        public void Add(T t)
        {
            _items.Add(t);
        }

        protected abstract IEnumerator Process(IEnumerable<T> ts);

        public IEnumerator DoProcess()
        {
            while (true)
            {
                if (_items.Any())
                {
                    yield return Process(_items);
                    _items = new List<T>();
                }

                yield return new WaitForSeconds(PollPeriod);
            }
        }
    }
}
