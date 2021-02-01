using LuceRPG.Game.Models;
using LuceRPG.Game.Util;
using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldLoader : MonoBehaviour
{
    public static WorldLoader Instance = null;
    public GameObject WallPrefab = null;
    public GameObject PathPrefab = null;
    public GameObject PlayerPrefab = null;
    public GameObject BackgroundPrefab = null;
    public GameObject CameraPrefab = null;

    public WorldModule.Model World { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(GetComponent<WorldLoader>());
        }
        else
        {
            Instance = this;
        }
    }

    private GameObject GetPrefab(WithId.Model<WorldObjectModule.Payload> obj)
    {
        var t = WorldObjectModule.t(obj);

        if (t.IsWall)
        {
            return WallPrefab;
        }
        else if (t.IsPath)
        {
            return PathPrefab;
        }
        else if (t.IsPlayer)
        {
            return PlayerPrefab;
        }

        return null;
    }

    private GameObject AddObject(WithId.Model<WorldObjectModule.Payload> obj)
    {
        var location = obj.GetGameLocation();
        var prefab = GetPrefab(obj);

        if (prefab != null)
        {
            var go = Instantiate(prefab, location, Quaternion.identity);

            if (!go.TryGetComponent<UniversalController>(out var uc))
            {
                uc = go.AddComponent<UniversalController>();
            }

            Debug.Log($"Setting UC ID to {obj.id}");
            uc.Id = obj.id;
            uc.Model = obj.value;

            if (WorldObjectModule.t(obj).IsPath)
            {
                var size = WorldObjectModule.size(obj.value);
                var spriteRenderer = go.GetComponent<SpriteRenderer>();
                spriteRenderer.size = new Vector2(size.x, size.y);
            }

            return go;
        }
        else
        {
            return null;
        }
    }

    public void LoadWorld(string playerId, WorldModule.Model world)
    {
        World = world;

        foreach (var bound in world.bounds)
        {
            var location = bound.GetGameLocation();

            if (BackgroundPrefab != null)
            {
                var bg = Instantiate(BackgroundPrefab, location, Quaternion.identity);
                var spriteRenderer = bg.GetComponent<SpriteRenderer>();

                if (spriteRenderer != null)
                {
                    var size = new Vector2(bound.size.x, bound.size.y);
                    spriteRenderer.size = size;
                }
            }
        }

        var objectCount = world.objects.Count;
        Debug.Log($"Loading {objectCount} objects");

        foreach (var kvp in world.objects)
        {
            var obj = kvp.Value;
            var go = AddObject(obj);

            if (obj.id == playerId && go != null)
            {
                Debug.Log($"Adding PC to {obj.id}");
                go.AddComponent<PlayerController>();
                Instantiate(CameraPrefab, go.transform);
            }
        }
    }

    public void ApplyUpdate(
        IEnumerable<WorldEventModule.Model> worldEvents,
        UpdateSource source
    )
    {
        foreach (var worldEvent in worldEvents)
        {
            if (source == UpdateSource.Server
                && OptimisticIntentionProcessor.Instance.DidProcess(worldEvent.resultOf))
            {
                OptimisticIntentionProcessor.Instance.CheckEvent(worldEvent);
                continue;
            }

            World = EventApply.apply(worldEvent, World);

            var tObjectId = WorldEventModule.getObjectId(worldEvent.t);
            if (tObjectId.HasValue())
            {
                var objectId = tObjectId.Value;

                if (worldEvent.t.IsObjectAdded)
                {
                    var objectAdded = ((WorldEventModule.Type.ObjectAdded)worldEvent.t).Item;
                    AddObject(objectAdded);
                    Debug.Log($"Adding item to game {objectAdded.id}");
                }
                else
                {
                    var uc = UniversalController.GetById(objectId);
                    if (uc != null)
                    {
                        uc.Apply(worldEvent);
                    }
                    else
                    {
                        Debug.LogError($"Could not process update for unknown object {objectId}");
                    }
                }
            }
        }
    }

    public void CheckConsistency(WorldModule.Model world)
    {
        // Check whether all game objects exist and are in the correct place
        //   snap them to the location if not
        foreach (var modelObj in WorldModule.objectList(world))
        {
            var uc = UniversalController.GetById(modelObj.id);

            if (uc == null)
            {
                Debug.Log($"Adding missing object {modelObj.id}");
                AddObject(modelObj);
            }
            else
            {
                var expectedLocation = modelObj.GetGameLocation();
                uc.EnsureLocation(expectedLocation);
            }
        }

        // Check whether no extra world objects exist
        foreach (var gameObj in UniversalController.Controllers)
        {
            var gameObjId = gameObj.Key;
            var isInModel = MapModule.ContainsKey(gameObjId, world.objects);

            if (!isInModel)
            {
                Debug.Log($"Removing extra object {gameObjId}");
                Destroy(gameObj.Value.gameObject);
            }
        }
    }
}
