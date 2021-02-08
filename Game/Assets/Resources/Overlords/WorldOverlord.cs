using LuceRPG.Game.Util;
using LuceRPG.Game.WorldObjects;
using LuceRPG.Models;
using System.Collections;
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
            Registry.Streams.WorldEvents.RegisterOnAdd(AddObject);
            Registry.Streams.WorldEvents.RegisterOnUcEvent(OnUcEvent);

            Registry.Services.ConfigLoader.LoadConfig();

            StartCoroutine(OnStarted());
        }

        private IEnumerator OnStarted()
        {
            yield return Registry.Services.WorldLoader.LoadWorld(LoadWorldGameObjects);
            yield return Registry.Services.WorldLoader.GetUpdates(OnDiff);
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
                    AddObject(obj);
                }
            }
        }

        private void AddObject(WithId.Model<WorldObjectModule.Payload> obj)
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

        private void OnDiff(WorldDiffModule.DiffType diff)
        {
            Debug.Log(diff);
        }
    }
}
