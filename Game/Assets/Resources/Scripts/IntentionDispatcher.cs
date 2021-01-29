using LuceRPG.Models;
using UnityEngine;

public class IntentionDispatcher : MonoBehaviour
{
    public static IntentionDispatcher Instance = null;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(GetComponent<IntentionDispatcher>());
        }
        else
        {
            Instance = this;
        }
    }

    public void Dispatch(IntentionModule.Type t)
    {
        var id = OptimisticIntentionProcessor.Instance.Process(t);

        StartCoroutine(Registry.CommsService.SendIntention(id, t));
    }
}
