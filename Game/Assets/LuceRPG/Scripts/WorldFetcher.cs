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
    public float PollPeriod = 0.1f;
    public string BaseUrl = "https://localhost:5001/World/";

    private void Start()
    {
        StartCoroutine(FetchWorld());
    }

    private IEnumerator FetchWorld()
    {
        void OnUpdate(GetSinceResultModule.Payload update)
        {
            if (update.IsEvents)
            {
                var events =
                    ((GetSinceResultModule.Payload.Events)update)
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

        void OnLoad(string playerId, WorldModule.Model world)
        {
            if (WorldLoader.Instance != null)
            {
                Debug.Log("Loading world");
                WorldLoader.Instance.LoadWorld(playerId, world);
            }
        }

        return CommsService.Instance.JoinGame(OnLoad, OnUpdate);
    }
}
