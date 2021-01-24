using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using LuceRPG.Models;
using LuceRPG.Serialisation;
using LuceRPG.Utility;
using System.Linq;
using System;

public class WorldFetcher : MonoBehaviour
{
    private long _lastTimestamp = 0;
    public float PollPeriod = 0.1f;
    public string BaseUrl = "https://localhost:5001/World/";

    private void Start()
    {
        StartCoroutine(FetchWorld());
    }

    private IEnumerator FetchWorld()
    {
        Debug.Log("Attempting to load world");
        var webRequest = UnityWebRequest.Get(BaseUrl + "join");
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            var bytes = webRequest.downloadHandler.data;

            var tResult = GetJoinGameResultSrl.deserialise(bytes);

            if (tResult.HasValue())
            {
                var result = tResult.Value.value;

                if (result.IsSuccess)
                {
                    var success = (GetJoinGameResultModule.Model.Success)result;
                    var clientId = success.Item1;
                    IntentionDispatcher.Instance.ClientId = clientId;
                    var playerId = success.Item2;
                    Debug.Log($"Client {clientId} joined with player ID {playerId}");
                    var tsWorld = success.Item3;

                    _lastTimestamp = tsWorld.timestamp;

                    if (WorldLoader.Instance != null)
                    {
                        Debug.Log("Loading world");
                        WorldLoader.Instance.LoadWorld(tsWorld.value);

                        yield return FetchUpdates();
                    }
                }
                else
                {
                    var failure = (GetJoinGameResultModule.Model.Failure)result;
                    Debug.LogError($"Could not join world {failure.Item}");
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

            var webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var bytes = webRequest.downloadHandler.data;

                var tUpdate = GetSinceResultSrl.deserialise(bytes);

                if (tUpdate.HasValue())
                {
                    var update = tUpdate.Value.value;
                    _lastTimestamp = update.timestamp;

                    if (update.value.IsEvents)
                    {
                        var events =
                            ((GetSinceResultModule.Payload.Events)update.value)
                            .Item
                            .Select(e => e.value)
                            .ToArray();

                        if (events.Any())
                        {
                            Debug.Log($"Updating world with {events.Length} events");
                            WorldLoader.Instance.ApplyUpdate(events);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException("Updating from entire world state is not yet supported");
                    }
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
