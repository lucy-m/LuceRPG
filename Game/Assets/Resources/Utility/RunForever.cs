using System;
using System.Collections;
using UnityEngine;

namespace LuceRPG.Game.Utility
{
    public static class CoroutineUtil
    {
        public static IEnumerator RunForever(Action fn, float period)
        {
            while (true)
            {
                fn();

                yield return new WaitForSeconds(period);
            }
        }

        public static IEnumerator RunForever(Func<IEnumerator> fn, float period)
        {
            while (true)
            {
                yield return fn();

                yield return new WaitForSeconds(period);
            }
        }
    }
}
