using LuceRPG.Models;
using LuceRPG.Utility;
using System.Collections;
using UnityEngine;

namespace LuceRPG.Game.Overlords
{
    public class PlayerOverlord : MonoBehaviour
    {
        private ITimestampProvider TimestampProvider => Registry.TimestampProvider;
        private string Id => Registry.Stores.World.PlayerId;

        // Start is called before the first frame update
        private void Start()
        {
            StartCoroutine(PollInput());
        }

        private IEnumerator PollInput()
        {
            while (true)
            {
                var vertIn = Registry.Providers.Input.GetVertIn();
                var horzIn = Registry.Providers.Input.GetHorzIn();

                if (vertIn > 0)
                {
                    var intention = IntentionModule.Type.NewMove(Id, DirectionModule.Model.North, 1);
                    Registry.Services.Intentions.Dispatch(intention);
                    yield return SpinWhileBusy();
                }

                if (vertIn < 0)
                {
                    var intention = IntentionModule.Type.NewMove(Id, DirectionModule.Model.South, 1);
                    Registry.Services.Intentions.Dispatch(intention);
                    yield return SpinWhileBusy();
                }

                if (horzIn > 0)
                {
                    var intention = IntentionModule.Type.NewMove(Id, DirectionModule.Model.East, 1);
                    Registry.Services.Intentions.Dispatch(intention);
                    yield return SpinWhileBusy();
                }

                if (horzIn < 0)
                {
                    var intention = IntentionModule.Type.NewMove(Id, DirectionModule.Model.West, 1);
                    Registry.Services.Intentions.Dispatch(intention);
                    yield return SpinWhileBusy();
                }

                yield return null;
            }
        }

        public IEnumerator SpinWhileBusy()
        {
            var busyUntil = Registry.Processors.Intentions.BusyUntil(Id);

            if (!busyUntil.HasValue)
            {
                yield return null;
            }
            else
            {
                while (TimestampProvider.Now < busyUntil.Value)
                {
                    yield return null;
                }
            }
        }
    }
}
