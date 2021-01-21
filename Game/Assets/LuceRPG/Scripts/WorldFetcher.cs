using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using LuceRPG.Models;
using LuceRPG.Serialisation;
using Microsoft.FSharp.Core;
using LuceRPG.Utility;

public class WorldFetcher : MonoBehaviour
{
    private long _lastTimestamp = 0;
    public float PollPeriod = 1;
    public string BaseUrl = "https://localhost:5001/World/";

    private void Start()
    {
        StartCoroutine(FetchWorld());
        StartCoroutine(FetchUpdates());
    }

    private IEnumerator FetchWorld()
    {
        Debug.Log("Attempting to load world");
        var webRequest = UnityWebRequest.Get(BaseUrl);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            var bytes = webRequest.downloadHandler.data;

            var tWorld = WithTimestampSrl.deserialise(
                FSharpFunc<byte[], FSharpOption<DesrlResult.Payload<WorldModule.Model>>>.FromConverter(
                    WorldSrl.deserialise
                ),
                bytes
            );

            if (tWorld.HasValue())
            {
                _lastTimestamp = tWorld.Value.value.timestamp;
                var world = tWorld.Value.value.value;

                if (WorldLoader.Instance != null)
                {
                    Debug.Log("Loading world");
                    WorldLoader.Instance.LoadWorld(world);
                }
            }
            else
            {
                Debug.LogError("Could not deserialise world");
            }
        }
        else
        {
            Debug.LogError("Web request error " + webRequest.error);
        }
    }

    private IEnumerator FetchUpdates()
    {
        while (true)
        {
            var url = BaseUrl + "since?timestamp=" + _lastTimestamp;
            Debug.Log("Polling for updates since " + _lastTimestamp);

            var webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var bytes = webRequest.downloadHandler.data;
                Debug.Log("Received bytes " + bytes.Length);

                var tUpdate = GetSinceResultSrl.deserialise(bytes);

                if (tUpdate.HasValue())
                {
                    var update = tUpdate.Value.value;
                    _lastTimestamp = update.timestamp;
                    Debug.Log("Got update " + update);
                }
                else
                {
                    Debug.LogError("Could not deserialise update");
                }
            }
            else
            {
                Debug.LogError("Web request error " + webRequest.error);
            }

            yield return new WaitForSeconds(PollPeriod);
        }
    }
}
