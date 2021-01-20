using LuceRPG.Models;
using LuceRPG.Serialisation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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

    public void Dispatch(IntentionModule.Model intention)
    {
        StartCoroutine(SendIntention(intention));
    }

    private IEnumerator SendIntention(IntentionModule.Model intention)
    {
        Debug.Log("Attempting to send intention");
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
