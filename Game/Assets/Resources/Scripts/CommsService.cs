using LuceRPG.Models;
using LuceRPG.Serialisation;
using LuceRPG.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public interface ICommsService
{
    IEnumerator JoinGame(
        Action<string, WorldModule.Model> onLoad,
        Action<GetSinceResultModule.Payload> onUpdate,
        Action<WorldModule.Model> onConsistencyCheck
    );

    IEnumerator SendIntention(string id, IntentionModule.Type t);
}

public class CommsService : ICommsService
{
    public float PollPeriod = 0.1f;
    public int ConsistencyCheckFreq = 15;
    private string BaseUrl => Registry.ConfigLoader.Config.BaseUrl;
    private string Username => Registry.ConfigLoader.Config.Username;
    private string Password => Registry.ConfigLoader.Config.Password;

    private string _clientId = null;

    private readonly ITimestampProvider _timestampProvider = new TimestampProvider();

    public IEnumerator JoinGame(
        Action<string, WorldModule.Model> onLoad,
        Action<GetSinceResultModule.Payload> onUpdate,
        Action<WorldModule.Model> onConsistencyCheck
    )
    {
        var url =
            BaseUrl
            + "World/join"
            + "?username=" + Username
            + "&password=" + Password;

        Debug.Log($"Attempting to load world at {BaseUrl}");
        var webRequest = UnityWebRequest.Get(url);
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

                    yield return FetchUpdates(tsWorld.timestamp, onUpdate, onConsistencyCheck);
                }
                else if (result.IsIncorrectCredentials)
                {
                    Debug.LogError("Credentials are incorrect, please update your config.json");
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

    private IEnumerator FetchUpdate(
        long timestamp,
        Action<GetSinceResultModule.Payload> onUpdate,
        Action<long> updateTimestamp
    )
    {
        var url =
            BaseUrl
            + "World/since?timestamp=" + timestamp
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
                updateTimestamp(update.timestamp);
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
    }

    private IEnumerator ConsistencyCheck(Action<WorldModule.Model> onConsistencyCheck)
    {
        Debug.Log("Doing consistency check");

        var url =
            BaseUrl
            + "World/allState?clientId=" + _clientId;

        var webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            var bytes = webRequest.downloadHandler.data;
            var tUpdate = WorldSrl.deserialise(bytes);

            if (tUpdate.HasValue())
            {
                onConsistencyCheck(tUpdate.Value.value);
            }
        }
        else
        {
            Debug.LogError("Web request error " + webRequest.error);
        }
    }

    private IEnumerator FetchUpdates(
        long initialTimestamp,
        Action<GetSinceResultModule.Payload> onUpdate,
        Action<WorldModule.Model> onConsistencyCheck
    )
    {
        var lastTimestamp = initialTimestamp;
        var lastConsistencyCheck = lastTimestamp;
        var checkTicks = TimeSpan.FromSeconds(ConsistencyCheckFreq).Ticks;

        while (true)
        {
            if (lastTimestamp - lastConsistencyCheck > checkTicks)
            {
                yield return ConsistencyCheck(onConsistencyCheck);

                lastConsistencyCheck += checkTicks;
            }
            else
            {
                var prior = _timestampProvider.Now;
                yield return FetchUpdate(lastTimestamp, onUpdate, ts => lastTimestamp = ts);
                var post = _timestampProvider.Now;

                var ping = TimeSpan.FromTicks(post - prior).Milliseconds;
                UIStatsOverlay.Instance.SetPingMs(ping);
            }

            yield return new WaitForSeconds(PollPeriod);
        }
    }

    public IEnumerator SendIntention(string id, IntentionModule.Type t)
    {
        if (_clientId != null)
        {
            var intention = WithId.useId(id, IntentionModule.makePayload(_clientId, t));

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

public class TestCommsService : ICommsService
{
    public Action<string, WorldModule.Model> OnLoad { get; private set; }
    public Action<GetSinceResultModule.Payload> OnUpdate { get; private set; }
    public Action<WorldModule.Model> OnConsistencyCheck { get; private set; }
    public IntentionModule.Type LastIntention { get; private set; }
    public List<IntentionModule.Type> AllIntentions { get; } = new List<IntentionModule.Type>();

    public IEnumerator JoinGame(
        Action<string, WorldModule.Model> onLoad,
        Action<GetSinceResultModule.Payload> onUpdate,
        Action<WorldModule.Model> onConsistencyCheck
    )
    {
        OnLoad = onLoad;
        OnUpdate = onUpdate;
        OnConsistencyCheck = onConsistencyCheck;
        yield return null;
    }

    public IEnumerator SendIntention(string id, IntentionModule.Type t)
    {
        LastIntention = t;
        AllIntentions.Add(t);
        yield return null;
    }
}
