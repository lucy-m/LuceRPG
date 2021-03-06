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
        public GameObject NpcPrefab = null;
        public GameObject WarpPrefab = null;
        public BackgroundController BackgroundPrefab = null;
        public GameObject CameraPrefab = null;
        public GameObject UnitNamePrefab = null;
        public Canvas WorldTextCanvas = null;

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
                OnWorldChanged,
                we => OnWorldEvent(we, UpdateSource.Server),
                OnDiff);
        }

        private GameObject GetPrefab(WithId.Model<WorldObjectModule.Payload> obj)
        {
            var t = obj.value.t;

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
            else if (t.IsWarp)
            {
                return WarpPrefab;
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
                    var bc = Instantiate(BackgroundPrefab, bound.btmLeft.ToVector3(), Quaternion.identity);
                    bc.Rect = bound;
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

        private void OnWorldChanged()
        {
            StartCoroutine(OnWorldChangedEnum());
        }

        private IEnumerator OnWorldChangedEnum()
        {
            // Delete all existing objects
            foreach (var kvp in UniversalController.Controllers)
            {
                var uc = kvp.Value;
                Destroy(uc.gameObject);
            }

            var bgs = GameObject.FindObjectsOfType<BackgroundController>();
            foreach (var bg in bgs)
            {
                Destroy(bg.gameObject);
            }

            yield return null;

            LoadWorldGameObjects();
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
                    if (worldEvent.t.IsObjectAdded)
                    {
                        var objectAdded = ((WorldEventModule.Type.ObjectAdded)worldEvent.t).Item;
                        AddObject(objectAdded);
                    }
                }
            }
        }

        private void AddObject(WithId.Model<WorldObjectModule.Payload> obj)
        {
            var location = obj.value.btmLeft.ToVector3();
            var prefab = GetPrefab(obj);

            if (prefab != null)
            {
                var go = Instantiate(prefab, location, Quaternion.identity);

                if (!go.TryGetComponent<UniversalController>(out var uc))
                {
                    uc = go.AddComponent<UniversalController>();
                }

                uc.Id = obj.id;
                uc.SetModelProps(obj.value);

                if (obj.value.t.IsPath)
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

                var tName = WorldObjectModule.getName(obj.value);
                if (tName.HasValue())
                {
                    var unitNameLocation = obj.GetCenterLocation() + Vector3.up;
                    var unitNameGo = Instantiate(UnitNamePrefab, unitNameLocation, Quaternion.identity, WorldTextCanvas.transform);
                    var unitName = unitNameGo.GetComponent<UnitNameController>();
                    unitName.SetFollow(tName.Value, uc);
                }

                var caController = go.GetComponent<CharacterAppearanceController>();
                var charData = WorldObjectModule.getCharacterData(obj.value);

                if (caController != null && charData.HasValue())
                {
                    caController.CharacterData = charData.Value;
                }
            }
        }

        private void OnDiff(
            WorldModule.Payload world,
            IReadOnlyCollection<WorldDiffModule.DiffType> diffs)
        {
            Registry.Stores.World.World = world;

            foreach (var diff in diffs)
            {
                if (diff.IsMissingObject)
                {
                    var extraObject = ((WorldDiffModule.DiffType.MissingObject)diff).Item;
                    var uc = UniversalController.GetById(extraObject);
                    Destroy(uc.gameObject);
                }
                else if (diff.IsExtraObject)
                {
                    var missingObjectId = ((WorldDiffModule.DiffType.ExtraObject)diff).Item;
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
