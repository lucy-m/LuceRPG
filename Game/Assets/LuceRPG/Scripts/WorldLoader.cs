using LuceRPG.Models;
using UnityEngine;

public class WorldLoader : MonoBehaviour
{
    public static WorldLoader Instance = null;
    public GameObject WallPrefab = null;
    public GameObject PathPrefab = null;
    public GameObject PlayerPrefab = null;
    public GameObject BackgroundPrefab = null;

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

    private GameObject GetPrefab(WorldObjectModule.Model obj)
    {
        if (obj.t.IsWall)
        {
            return WallPrefab;
        }
        else if (obj.t.IsPath)
        {
            return PathPrefab;
        }
        else if (obj.t.IsPlayer)
        {
            return PlayerPrefab;
        }

        return null;
    }

    public void LoadWorld(WorldModule.Model world)
    {
        foreach (var kvp in world.objects)
        {
            var obj = kvp.Value;

            var location = obj.GetGameLocation();
            var prefab = GetPrefab(obj);

            if (prefab != null)
            {
                var go = Instantiate(prefab, location, Quaternion.identity);

                var pc = go.GetComponent<PlayerController>();

                if (pc != null)
                {
                    Debug.Log("Setting player ID");
                    pc.Id = obj.id;
                }

                if (obj.t.IsPath)
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
}
