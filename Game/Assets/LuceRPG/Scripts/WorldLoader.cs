using LuceRPG.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldLoader : MonoBehaviour
{
    public static WorldLoader Instance = null;
    public GameObject WallPrefab = null;
    public GameObject PathPrefab = null;
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

    public void LoadWorld(WorldModule.Model world)
    {
        foreach (var obj in world.objects)
        {
            var location = obj.GetGameLocation();
            var prefab = obj.t.IsWall ? WallPrefab : PathPrefab;

            if (prefab != null)
            {
                Instantiate(prefab, location, Quaternion.identity);
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
