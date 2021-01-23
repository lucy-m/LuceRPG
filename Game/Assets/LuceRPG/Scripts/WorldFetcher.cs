using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using LuceRPG.Models;
using LuceRPG.Serialisation;
using Microsoft.FSharp.Core;
using LuceRPG.Utility;
using System.Linq;

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
        var webRequest = UnityWebRequest.Get(BaseUrl);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            var bytes = webRequest.downloadHandler.data;

            var bytes28 = bytes.Skip(28).ToArray();
            var list = ListSrl.deserialise(
                    FSharpFunc<byte[], FSharpOption<DesrlResult.Payload<WithId.Model<WorldObjectModule.Payload>>>>
                    .FromConverter(WorldObjectSrl.deserialise),
                    bytes28);

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

                    yield return FetchUpdates();
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
