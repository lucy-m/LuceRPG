using LuceRPG.Models;
using LuceRPG.Serialisation;
using LuceRPG.Utility;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class CommsService : MonoBehaviour
{
    public static CommsService Instance = null;

    public float PollPeriod = 0.1f;
    private string BaseUrl => ConfigLoader.Instance.Config.BaseUrl;

    private string _clientId = null;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(GetComponent<CommsService>());
        }
        else
        {
            Instance = this;
        }
    }

    public IEnumerator JoinGame(
        Action<string, WorldModule.Model> onLoad,
        Action<GetSinceResultModule.Payload> onUpdate
    )
    {
        Debug.Log($"Attempting to load world at {BaseUrl}");
        var webRequest = UnityWebRequest.Get(BaseUrl + "World/join");
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
                    var playerId = success.Item2;
                    var tsWorld = success.Item3;

                    Debug.Log($"Client {clientId} joined with player ID {playerId}");

                    _clientId = clientId;

                    onLoad(playerId, tsWorld.value);

                    yield return FetchUpdates(tsWorld.timestamp, onUpdate);
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

    private IEnumerator FetchUpdates(
        long initialTimestamp,
        Action<GetSinceResultModule.Payload> onUpdate
    )
    {
        var lastTimestamp = initialTimestamp;

        while (true)
        {
            var url =
                BaseUrl
                + "World/since?timestamp=" + lastTimestamp
                + "&clientId=" + _clientId;

            var webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var bytes = webRequest.downloadHandler.data;

                var tUpdate = GetSinceResultSrl.deserialise(bytes);

                if (tUpdate.HasValue())
                {
                    var update = tUpdate.Value.value;
                    lastTimestamp = update.timestamp;
                    onUpdate(update.value);
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

    public IEnumerator SendIntention(IntentionModule.Type t)
    {
        if (_clientId != null)
        {
            var intention = WithId.create(IntentionModule.makePayload(_clientId, t));

            var bytes = IntentionSrl.serialise(intention);
            var webRequest = UnityWebRequest.Put(BaseUrl + "World/intention", bytes);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Web request error " + webRequest.error);
            }
        }
        else
        {
            Debug.LogError("No client ID available");
        }
    }
}
