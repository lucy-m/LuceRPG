using LuceRPG.Models;
using System.Collections.Generic;
using UnityEngine;

public class UniversalController : MonoBehaviour
{
    private readonly static Dictionary<string, UniversalController> Controllers
        = new Dictionary<string, UniversalController>();

    private string _id = "";

    public string Id
    {
        get => _id;
        set
        {
            Controllers.Remove(_id);
            Controllers[value] = this;

            _id = value;
        }
    }

    public static UniversalController GetById(string id)
    {
        if (Controllers.TryGetValue(id, out var uc))
        {
            return uc;
        }
        else
        {
            return null;
        }
    }

    public float Speed = 4;

    public Vector3 Target { get; private set; }

    private void Start()
    {
        Target = transform.position;
    }

    private void OnDestroy()
    {
        Controllers.Remove(_id);
    }

    public void Apply(WorldEventModule.Model worldEvent)
    {
        if (worldEvent.t.IsMoved)
        {
            var moved = (WorldEventModule.Type.Moved)worldEvent.t;

            var direction = moved.Item2;
            var amount = moved.Item3;

            if (direction.IsNorth)
            {
                Target += new Vector3(0, amount);
            }
            else if (direction.IsSouth)
            {
                Target += new Vector3(0, -amount);
            }
            else if (direction.IsEast)
            {
                Target += new Vector3(amount, 0);
            }
            else if (direction.IsWest)
            {
                Target += new Vector3(-amount, 0);
            }
        }
        else if (worldEvent.t.IsObjectRemoved)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        var newPosition = Vector3.MoveTowards(transform.position, Target, Speed * Time.deltaTime);
        transform.position = newPosition;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawCube(Target, Vector3.one);
    }
}
