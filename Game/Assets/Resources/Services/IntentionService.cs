using LuceRPG.Models;
using System.Collections;
using UnityEngine;

namespace LuceRPG.Game.Services
{
    public class IntentionService
    {
        public IEnumerator Dispatch(IntentionModule.Type t)
        {
            var id = Registry.Processors.Intentions.Process(t);

            yield return Registry.Services.Comms.SendIntention(id, t);
        }
    }
}
