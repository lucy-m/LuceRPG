using LuceRPG.Models;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(UniversalController))]
public class PlayerController : MonoBehaviour
{
    private UniversalController _uc;
    public float InputDelay = 0.3f;

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
            var vertIn = Input.GetAxis("Vertical");
            var horzIn = Input.GetAxis("Horizontal");

            if (vertIn > 0)
            {
                var intention = IntentionModule.Payload.NewMove(_uc.Id, DirectionModule.Model.North, 1);
                IntentionDispatcher.Instance.Dispatch(intention);
                yield return new WaitForSeconds(InputDelay);
            }
            else if (vertIn < 0)
            {
                var intention = IntentionModule.Payload.NewMove(_uc.Id, DirectionModule.Model.South, 1);
                IntentionDispatcher.Instance.Dispatch(intention);
                yield return new WaitForSeconds(InputDelay);
            }
            else if (horzIn > 0)
            {
                var intention = IntentionModule.Payload.NewMove(_uc.Id, DirectionModule.Model.East, 1);
                IntentionDispatcher.Instance.Dispatch(intention);
                yield return new WaitForSeconds(InputDelay);
            }
            else if (horzIn < 0)
            {
                var intention = IntentionModule.Payload.NewMove(_uc.Id, DirectionModule.Model.West, 1);
                IntentionDispatcher.Instance.Dispatch(intention);
                yield return new WaitForSeconds(InputDelay);
            }

            yield return null;
        }
    }
}
