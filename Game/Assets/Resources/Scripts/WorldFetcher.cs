using LuceRPG.Game.Models;
using LuceRPG.Models;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class WorldFetcher : MonoBehaviour
{
    public float PollPeriod = 0.1f;

    public WorldStore WorldStore => Registry.WorldStore;

    private void Start()
    {
        StartCoroutine(FetchWorld());
    }

    private IEnumerator FetchWorld()
    {
        if (!WorldStore.HasWorld())
        {
            yield return Registry.CommsService.LoadGame((s, w) => { }, err => Debug.LogError(err));
        }

        if (WorldStore.HasWorld())
        {
            WorldLoader.Instance.LoadWorld(WorldStore.PlayerId, WorldStore.World);

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

            void OnConsistencyCheck(WorldModule.Model world)
            {
                WorldLoader.Instance.CheckConsistency(world);
            }

            yield return Registry.CommsService.FetchUpdates(OnUpdate, OnConsistencyCheck);
        }

        Debug.LogError("No world in store");
        yield return null;
    }
}
