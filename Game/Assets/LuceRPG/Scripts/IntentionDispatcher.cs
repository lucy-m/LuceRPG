using LuceRPG.Models;
using LuceRPG.Serialisation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class IntentionDispatcher : MonoBehaviour
{
    public static IntentionDispatcher Instance = null;
    public float PollPeriod = 0.5f;

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

    public void Dispatch(IntentionModule.Payload payload)
    {
        var intention = WithId.create(payload);

        StartCoroutine(SendIntention(intention));
    }

    private IEnumerator SendIntention(WithId.Model<IntentionModule.Payload> intention)
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
