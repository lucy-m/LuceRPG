using LuceRPG.Game.Processors;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LuceRPG.Game.Overlords
{
    public class ProcessorOverlord : MonoBehaviour
    {
        private readonly IReadOnlyCollection<IEnumerator> _processors
            = new List<IEnumerator>
            {
                Registry.Processors.Logs.DoProcess(),
                Registry.Processors.Intentions.DoProcess()
            };

        private void Start()
        {
            foreach (var p in _processors)
            {
                StartCoroutine(p);
            }
        }
    }
}
