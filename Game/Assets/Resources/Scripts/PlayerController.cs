using LuceRPG.Models;
using LuceRPG.Utility;
using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(UniversalController))]
public class PlayerController : MonoBehaviour
{
    private UniversalController _uc;

    private readonly ITimestampProvider _timestampProvider = new TimestampProvider();

    // Start is called before the first frame update
    private void Start()
    {
        _uc = GetComponent<UniversalController>();
        StartCoroutine(PollInput());
    }

    private IEnumerator PollInput()
    {
        while (true)
        {
            var vertIn = Registry.InputProvider.GetVertIn();
            var horzIn = Registry.InputProvider.GetHorzIn();

            if (vertIn > 0)
            {
                var intention = IntentionModule.Type.NewMove(_uc.Id, DirectionModule.Model.North, 1);
                IntentionDispatcher.Instance.Dispatch(intention);
                yield return SpinWhileBusy();
            }

            if (vertIn < 0)
            {
                var intention = IntentionModule.Type.NewMove(_uc.Id, DirectionModule.Model.South, 1);
                IntentionDispatcher.Instance.Dispatch(intention);
                yield return SpinWhileBusy();
            }

            if (horzIn > 0)
            {
                var intention = IntentionModule.Type.NewMove(_uc.Id, DirectionModule.Model.East, 1);
                IntentionDispatcher.Instance.Dispatch(intention);
                yield return SpinWhileBusy();
            }

            if (horzIn < 0)
            {
                var intention = IntentionModule.Type.NewMove(_uc.Id, DirectionModule.Model.West, 1);
                IntentionDispatcher.Instance.Dispatch(intention);
                yield return SpinWhileBusy();
            }

            yield return null;
        }
    }

    public IEnumerator SpinWhileBusy()
    {
        var busyUntil = OptimisticIntentionProcessor.Instance.BusyUntil(_uc.Id);

        if (!busyUntil.HasValue)
        {
            yield return null;
        }
        else
        {
            while (_timestampProvider.Now < busyUntil.Value)
            {
                yield return null;
            }
        }
    }
}
