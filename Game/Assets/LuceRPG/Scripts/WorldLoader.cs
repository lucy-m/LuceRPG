using LuceRPG.Models;
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

    private Dictionary<Guid, UniversalController> _controllers;

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

    private GameObject GetPrefab(WithGuid.Model<WorldObjectModule.Payload> obj)
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

    public void LoadWorld(WorldModule.Model world)
    {
        _controllers = new Dictionary<Guid, UniversalController>();

        var objectCount = world.objects.Count;
        Debug.Log($"Loading {objectCount} objects");

        foreach (var kvp in world.objects)
        {
            var obj = kvp.Value;

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
                    var size = WorldObjectModule.size(obj);
                    var spriteRenderer = go.GetComponent<SpriteRenderer>();
                    spriteRenderer.size = new Vector2(size.x, size.y);
                }
            }
        }

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
    }

    public void ApplyUpdate(IEnumerable<WorldEventModule.Model> worldEvents)
    {
        foreach (var worldEvent in worldEvents)
        {
            if (_controllers.TryGetValue(worldEvent.Item1, out var uc))
            {
                uc.Apply(worldEvent);
            }
            else
            {
                Debug.LogError($"Could not process update for unknown object {worldEvent.Item1}");
            }
        }
    }
}
