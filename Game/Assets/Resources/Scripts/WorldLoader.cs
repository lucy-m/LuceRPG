using LuceRPG.Models;
using LuceRPG.Utility;
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

    private Dictionary<string, UniversalController> _controllers;

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

            var uc = go.GetComponent<UniversalController>();

            if (uc != null)
            {
                Debug.Log($"Setting UC ID to {obj.id}");
                uc.Id = obj.id;
                _controllers[obj.id] = uc;
            }

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
        _controllers = new Dictionary<string, UniversalController>();

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
            }
        }
    }

    public void ApplyUpdate(IEnumerable<WorldEventModule.Model> worldEvents)
    {
        foreach (var worldEvent in worldEvents)
        {
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
                    if (_controllers.TryGetValue(objectId, out var uc))
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
}
