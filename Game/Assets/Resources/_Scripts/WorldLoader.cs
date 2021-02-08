using LuceRPG.Game.Models;
using LuceRPG.Game.Util;
using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class _WorldLoader : MonoBehaviour
{
    public static WorldLoader Instance = null;
    public GameObject WallPrefab = null;
    public GameObject PathPrefab = null;
    public GameObject PlayerPrefab = null;
    public GameObject BackgroundPrefab = null;
    public GameObject CameraPrefab = null;
    public GameObject NpcPrefab = null;

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
        else if (t.IsNPC)
        {
            return NpcPrefab;
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

    public void CheckConsistency(WorldModule.Model world)
    {
        var diff = WorldDiffModule.diff(world, World).ToArray();
        if (diff.Any())
        {
            Debug.LogWarning($"Consistency check failed with {diff.Length} results");
            var logs = ClientLogEntryModule.Payload.NewConsistencyCheckFailed(ListModule.OfSeq(diff));
            LogDispatcher.Instance.AddLog(logs);
        }

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
