using LuceRPG.Adapters;
using LuceRPG.Game.Models;
using LuceRPG.Models;
using LuceRPG.Serialisation;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace LuceRPG.Game.Services
{
    public interface ICommsService
    {
        IEnumerator FetchUpdates(
            float consistencyCheckFreq,
            float pollPeriod,
            Action<WithTimestamp.Model<GetSinceResultModule.Payload>> onUpdate,
            Action<WorldModule.Payload> onConsistencyCheck);

        IEnumerator LoadWorld(Action<LoadWorldPayload> onLoad, Action<string> onError);

        IEnumerator SendIntention(string id, IntentionModule.Type t);

        IEnumerator SendLogs(IEnumerable<WithTimestamp.Model<ClientLogEntryModule.Payload>> logs);
    }

    public class CommsService : ICommsService
    {
        private string BaseUrl => Registry.Stores.Config.Config.BaseUrl;
        private string Username => Registry.Stores.Config.Config.Username;
        private string Password => Registry.Stores.Config.Config.Password;
        private string ClientId => Registry.Stores.World.ClientId;
        private long LastUpdate => Registry.Stores.World.LastUpdate;
        private ITimestampProvider TimestampProvider => Registry.Providers.Timestamp;

        public IEnumerator LoadWorld(
            Action<LoadWorldPayload> onLoad,
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

                        var payload = new LoadWorldPayload(clientId, playerId, tsWorld, interactions);
                        onLoad(payload);
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
            Action<WithTimestamp.Model<GetSinceResultModule.Payload>> onUpdate
        )
        {
            var url =
                BaseUrl
                + "World/since?timestamp=" + LastUpdate
                + "&clientId=" + ClientId;

            var preTs = TimestampProvider.Now;
            var webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            var postTs = TimestampProvider.Now;
            Registry.Stores.PerfStats.Ping = TimeSpan.FromTicks(postTs - preTs).Milliseconds;

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var bytes = webRequest.downloadHandler.data;

                var tUpdate = GetSinceResultSrl.deserialise(bytes);

                if (tUpdate.HasValue())
                {
                    var update = tUpdate.Value.value;
                    onUpdate(update);
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

        private IEnumerator ConsistencyCheck(Action<WorldModule.Payload> onConsistencyCheck)
        {
            Debug.Log("Doing consistency check");

            var url =
                BaseUrl
                + "World/allState?clientId=" + ClientId;

            var webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var bytes = webRequest.downloadHandler.data;
                var tUpdate = WorldSrl.deserialise(bytes);

                if (tUpdate.HasValue())
                {
                    onConsistencyCheck(tUpdate.Value.value.value);
                }
            }
            else
            {
                Debug.LogError("Web request error " + webRequest.error);
            }
        }

        public IEnumerator FetchUpdates(
            float consistencyCheckFreq,
            float pollPeriod,
            Action<WithTimestamp.Model<GetSinceResultModule.Payload>> onUpdate,
            Action<WorldModule.Payload> onConsistencyCheck
        )
        {
            var lastConsistencyCheck = LastUpdate;
            var checkTicks = TimeSpan.FromSeconds(consistencyCheckFreq).Ticks;

            while (true)
            {
                if (LastUpdate - lastConsistencyCheck > checkTicks)
                {
                    yield return ConsistencyCheck(onConsistencyCheck);

                    lastConsistencyCheck += checkTicks;
                }
                else
                {
                    yield return FetchUpdate(onUpdate);
                }

                yield return new WaitForSeconds(pollPeriod);
            }
        }

        public IEnumerator SendIntention(string id, IntentionModule.Type t)
        {
            if (ClientId != null)
            {
                var intention = WithId.useId(id, IntentionModule.makePayload(ClientId, t));

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
            if (ClientId != null)
            {
                var url =
                    BaseUrl
                    + "World/logs?clientId=" + ClientId;

                var bytes = ClientLogEntrySrl.serialiseLog(ListModule.OfSeq(logs));
                var webRequest = UnityWebRequest.Put(url, bytes);

                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Web request error " + webRequest.error);
                }
            }
        }
    }

    public class TestCommsService : ICommsService
    {
        public Action<WithTimestamp.Model<GetSinceResultModule.Payload>> OnUpdate { get; private set; }
        public Action<WorldModule.Payload> OnConsistencyCheck { get; private set; }
        public Action<string> OnLoadError { get; private set; }
        public string LastIntentionId { get; private set; }
        public IntentionModule.Type LastIntention { get; private set; }
        public List<IntentionModule.Type> AllIntentions { get; } = new List<IntentionModule.Type>();

        private bool loaded = false;
        private Action<LoadWorldPayload> _doLoad;

        public IEnumerator FetchUpdates(
            float consistencyCheckFreq,
            float pollPeriod,
            Action<WithTimestamp.Model<GetSinceResultModule.Payload>> onUpdate,
            Action<WorldModule.Payload> onConsistencyCheck)
        {
            OnUpdate = onUpdate;
            OnConsistencyCheck = onConsistencyCheck;

            yield return null;
        }

        public IEnumerator LoadWorld(Action<LoadWorldPayload> onLoad, Action<string> onError)
        {
            _doLoad = onLoad;
            OnLoadError = onError;
            while (!loaded)
            {
                yield return null;
            }
        }

        public void OnLoad(LoadWorldPayload payload)
        {
            loaded = true;
            _doLoad(payload);
        }

        public IEnumerator SendIntention(string id, IntentionModule.Type t)
        {
            Debug.Log($"Intention sent {t}");
            LastIntentionId = id;
            LastIntention = t;
            AllIntentions.Add(t);
            yield return null;
        }

        public IEnumerator SendLogs(IEnumerable<WithTimestamp.Model<ClientLogEntryModule.Payload>> logs)
        {
            yield return null;
        }
    }
}
