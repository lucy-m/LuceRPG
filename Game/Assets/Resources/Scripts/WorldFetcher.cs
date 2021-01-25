using LuceRPG.Models;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class WorldFetcher : MonoBehaviour
{
    public float PollPeriod = 0.1f;

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

        return Registry.CommsService.JoinGame(OnLoad, OnUpdate);
    }
}
