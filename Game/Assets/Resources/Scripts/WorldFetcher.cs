using LuceRPG.Game.Models;
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
                    WorldLoader.Instance.ApplyUpdate(events, UpdateSource.Server);
                }
            }
            else
            {
                var worldUpdate =
                    ((GetSinceResultModule.Payload.World)update)
                    .Item;

                WorldLoader.Instance.CheckConsistency(worldUpdate);
            }
        }

        void OnLoad(string playerId, WorldModule.Model world)
        {
            WorldLoader.Instance.LoadWorld(playerId, world);
        }

        void OnConsistencyCheck(WorldModule.Model world)
        {
            WorldLoader.Instance.CheckConsistency(world);
        }

        return Registry.CommsService.JoinGame(OnLoad, OnUpdate, OnConsistencyCheck);
    }
}
