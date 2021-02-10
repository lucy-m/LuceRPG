using LuceRPG.Game.Models;
using LuceRPG.Game.Utility;
using LuceRPG.Game.WorldObjects;
using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LuceRPG.Game.Overlords
{
    public class WorldOverlord : MonoBehaviour
    {
        public GameObject WallPrefab = null;
        public GameObject PathPrefab = null;
        public GameObject PlayerPrefab = null;
        public GameObject BackgroundPrefab = null;
        public GameObject CameraPrefab = null;
        public GameObject NpcPrefab = null;

        private void Start()
        {
            Registry.Processors.Intentions.RegisterOnEvent(we => OnWorldEvent(we, UpdateSource.Game));

            Registry.Services.ConfigLoader.LoadConfig();

            StartCoroutine(OnStarted());
        }

        private IEnumerator OnStarted()
        {
            yield return Registry.Services.WorldLoader.LoadWorld(LoadWorldGameObjects);
            yield return Registry.Services.WorldLoader.GetUpdates(
                we => OnWorldEvent(we, UpdateSource.Server),
                OnDiff);
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

        private void LoadWorldGameObjects()
        {
            var world = Registry.Stores.World.World;

            if (world == null)
            {
                throw new System.Exception("Unable to load world");
            }
            else
            {
                foreach (var bound in world.bounds)
                {
                    var location = bound.GetCenterLocation();

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
                    AddObject(obj);
                }
            }
        }

        private void OnWorldEvent(WorldEventModule.Model worldEvent, UpdateSource source)
        {
            if (source == UpdateSource.Server &&
                Registry.Processors.Intentions.DidProcess(worldEvent.resultOf))
            {
                var log = ClientLogEntryModule.Payload.NewUpdateIgnored(worldEvent);
                Registry.Processors.Logs.AddLog(log);
            }
            else
            {
                Registry.Stores.World.Apply(worldEvent);

                var tObjectId = WorldEventModule.getObjectId(worldEvent.t);
                if (tObjectId.HasValue())
                {
                    var objectId = tObjectId.Value;

                    if (worldEvent.t.IsObjectAdded)
                    {
                        var objectAdded = ((WorldEventModule.Type.ObjectAdded)worldEvent.t).Item;
                        AddObject(objectAdded);
                    }
                    else
                    {
                        OnUcEvent(objectId, worldEvent);
                    }
                }
            }
        }

        private void AddObject(WithId.Model<WorldObjectModule.Payload> obj)
        {
            var location = WorldObjectModule.btmLeft(obj).ToVector3();
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

                if (obj.id == Registry.Stores.World.PlayerId)
                {
                    Debug.Log($"Adding camera to {obj.id}");
                    Instantiate(CameraPrefab, go.transform);
                }
            }
        }

        private void OnUcEvent(string ucId, WorldEventModule.Model worldEvent)
        {
            var uc = UniversalController.GetById(ucId);
            if (uc != null)
            {
                uc.Apply(worldEvent);
            }
            else
            {
                Debug.LogError($"Could not process update for unknown object {ucId}");
            }
        }

        private void OnDiff(
            WorldModule.Model world,
            IReadOnlyCollection<WorldDiffModule.DiffType> diffs)
        {
            Registry.Stores.World.World = world;

            foreach (var diff in diffs)
            {
                if (diff.IsExtraObject)
                {
                    var extraObject = ((WorldDiffModule.DiffType.ExtraObject)diff).Item;
                    var uc = UniversalController.GetById(extraObject);
                    Destroy(uc.gameObject);
                }
                else if (diff.IsMissingObject)
                {
                    var missingObjectId = ((WorldDiffModule.DiffType.MissingObject)diff).Item;
                    var tMissingObject = MapModule.TryFind(missingObjectId, world.objects);

                    if (tMissingObject.HasValue())
                    {
                        AddObject(tMissingObject.Value);
                    }
                    else
                    {
                        Debug.LogError($"Unable to add missing object");
                    }
                }
                else if (diff.IsUnmatchingObjectPosition)
                {
                    var unmatching = (WorldDiffModule.DiffType.UnmatchingObjectPosition)diff;
                    var id = unmatching.Item1;
                    var location = unmatching.Item3.ToVector3();
                    var uc = UniversalController.GetById(id);
                    uc.EnsureLocation(location);
                }
                else
                {
                    Debug.LogError($"Unsupported diff type {diff} {diff.Tag}");
                }
            }
        }
    }
}
