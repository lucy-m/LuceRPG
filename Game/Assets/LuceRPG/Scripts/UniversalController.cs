using LuceRPG.Models;
using System;
using UnityEngine;

public class UniversalController : MonoBehaviour
{
    public string Id = "";
    public float Speed = 4;

    private Vector3 target;

    private void Start()
    {
        target = transform.position;
    }

    public void Apply(WorldEventModule.Model worldEvent)
    {
        var direction = worldEvent.Item2;
        var amount = worldEvent.Item3;

        if (direction.IsNorth)
        {
            target += new Vector3(0, amount);
        }
        else if (direction.IsSouth)
        {
            target += new Vector3(0, -amount);
        }
        else if (direction.IsEast)
        {
            target += new Vector3(amount, 0);
        }
        else if (direction.IsWest)
        {
            target += new Vector3(-amount, 0);
        }
    }

    private void Update()
    {
        var newPosition = Vector3.MoveTowards(transform.position, target, Speed * Time.deltaTime);
        transform.position = newPosition;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawCube(target, Vector3.one);
    }
}
