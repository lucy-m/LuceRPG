using LuceRPG.Adapters;
using LuceRPG.Models;
using LuceRPG.Serialisation;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public interface ICommsService
{
    IEnumerator FetchUpdates(Action<GetSinceResultModule.Payload> onUpdate, Action<WorldModule.Model> onConsistencyCheck);

    IEnumerator LoadGame(
        Action<string, WithTimestamp.Model<WorldModule.Model>> onLoad,
        Action<string> onError
    );

    IEnumerator SendIntention(string id, IntentionModule.Type t);

    IEnumerator SendLogs(IEnumerable<WithTimestamp.Model<ClientLogEntryModule.Payload>> logs);
}

public class CommsService : ICommsService
{
    public float PollPeriod = 0.1f;
    public int ConsistencyCheckFreq = 15;
    private string BaseUrl => Registry.ConfigLoader.Config.BaseUrl;
    private string Username => Registry.ConfigLoader.Config.Username;
    private string Password => Registry.ConfigLoader.Config.Password;

    private string _clientId = null;

    private ITimestampProvider TimestampProvider => Registry.TimestampProvider;

    public IEnumerator LoadGame(
        Action<string, WithTimestamp.Model<WorldModule.Model>> onLoad,
        Action<string> onError
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
                    var success = ((GetJoinGameResultModule.Model.Success)result).Item;

                    var clientId = success.clientId;
                    var playerId = success.playerObjectId;
                    var tsWorld = success.tsWorld;
                    var interactions = new InteractionStore(WithId.toMap(success.interactions));

                    Debug.Log($"Client {clientId} joined with player ID {playerId}");

                    _clientId = clientId;

                    Debug.Log("Loaded world from API");
                    Registry.WorldStore.PlayerId = playerId;
                    Registry.WorldStore.World = tsWorld.value;
                    Registry.WorldStore.LastUpdate = tsWorld.timestamp;
                    Registry.WorldStore.Interactions = interactions;

                    onLoad(playerId, tsWorld);
                }
                else if (result.IsIncorrectCredentials)
                {
                    onError("Credentials are incorrect, please update your config.json");
                }
                else
                {
                    var failure = (GetJoinGameResultModule.Model.Failure)result;
                    onError($"Could not join world {failure.Item}");
                }
            }
            else
            {
                onError("Could not deserialise world");
            }
        }
        else
        {
            onError("Web request error " + webRequest.error);
        }
    }

    private IEnumerator FetchUpdate(
        Action<GetSinceResultModule.Payload> onUpdate
    )
    {
        var url =
            BaseUrl
            + "World/since?timestamp=" + Registry.WorldStore.LastUpdate
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
                Registry.WorldStore.LastUpdate = update.timestamp;
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

    public IEnumerator FetchUpdates(
        Action<GetSinceResultModule.Payload> onUpdate,
        Action<WorldModule.Model> onConsistencyCheck
    )
    {
        var lastConsistencyCheck = Registry.WorldStore.LastUpdate;
        var checkTicks = TimeSpan.FromSeconds(ConsistencyCheckFreq).Ticks;

        while (true)
        {
            if (Registry.WorldStore.LastUpdate - lastConsistencyCheck > checkTicks)
            {
                yield return ConsistencyCheck(onConsistencyCheck);

                lastConsistencyCheck += checkTicks;
            }
            else
            {
                var prior = TimestampProvider.Now;
                yield return FetchUpdate(onUpdate);
                var post = TimestampProvider.Now;

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

    public IEnumerator SendLogs(IEnumerable<WithTimestamp.Model<ClientLogEntryModule.Payload>> logs)
    {
        var url =
            BaseUrl
            + "World/logs?clientId=" + _clientId;

        var bytes = ClientLogEntrySrl.serialiseLog(ListModule.OfSeq(logs));
        var webRequest = UnityWebRequest.Put(url, bytes);

        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Web request error " + webRequest.error);
        }
    }
}

public class TestCommsService : ICommsService
{
    public Action<string, WorldModule.Model> OnLoad { get; private set; }
    public Action<GetSinceResultModule.Payload> OnUpdate { get; private set; }
    public Action<WorldModule.Model> OnConsistencyCheck { get; private set; }
    public string LastIntentionId { get; private set; }
    public IntentionModule.Type LastIntention { get; private set; }
    public List<IntentionModule.Type> AllIntentions { get; } = new List<IntentionModule.Type>();

    public IEnumerator FetchUpdates(Action<GetSinceResultModule.Payload> onUpdate, Action<WorldModule.Model> onConsistencyCheck)
    {
        yield return null;
    }

    public IEnumerator LoadGame(
        Action<string, WithTimestamp.Model<WorldModule.Model>> onLoad,
        Action<string> onError
    )
    {
        yield return null;
    }

    public IEnumerator SendIntention(string id, IntentionModule.Type t)
    {
        LastIntention = t;
        LastIntentionId = id;
        AllIntentions.Add(t);
        yield return null;
    }

    public IEnumerator SendLogs(IEnumerable<WithTimestamp.Model<ClientLogEntryModule.Payload>> logs)
    {
        yield return null;
    }
}
