using LuceRPG.Models;
using LuceRPG.Serialisation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class IntentionDispatcher : MonoBehaviour
{
    public static IntentionDispatcher Instance = null;
    public float PollPeriod = 0.5f;

    private IntentionModule.Model _intention = null;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(GetComponent<IntentionDispatcher>());
        }
        else
        {
            Instance = this;
            //StartCoroutine(IntentionDispatcherLoop());
        }
    }

    public void Dispatch(IntentionModule.Model intention)
    {
        //_intention = intention;
        StartCoroutine(SendIntention(intention));
    }

    private IEnumerator IntentionDispatcherLoop()
    {
        while (true)
        {
            if (_intention != null)
            {
                var intention = _intention;
                _intention = null;
                yield return SendIntention(intention);
                yield return new WaitForSeconds(PollPeriod);
            }

            yield return null;
        }
    }

    private IEnumerator SendIntention(IntentionModule.Model intention)
    {
        var bytes = IntentionSrl.serialise(intention);
        var webRequest = UnityWebRequest.Put("https://localhost:5001/World/Intention", bytes);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Successfully sent intention");
        }
        else
        {
            Debug.LogError("Web request error " + webRequest.error);
        }
    }
}
