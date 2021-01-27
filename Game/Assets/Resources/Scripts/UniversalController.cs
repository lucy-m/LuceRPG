using LuceRPG.Game.Util;
using LuceRPG.Models;
using System.Collections.Generic;
using UnityEngine;

public class UniversalController : MonoBehaviour
{
    public readonly static Dictionary<string, UniversalController> Controllers
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

    private const float Speed = 4;

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
            var offset = DirectionUtil.AsVector3(moved.Item2, moved.Item3);

            Target += offset;
        }
        else if (worldEvent.t.IsObjectRemoved)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sets an object's target to the location or immediately
    ///   moves the object to the location if too far away
    /// </summary>
    /// <param name="location"></param>
    public void EnsureLocation(Vector3 location)
    {
        var distance = Vector3.Distance(location, Target);

        // If the object is close to its location then set the target
        //   and move normally
        if (distance < Speed)
        {
            Target = location;
        }
        // Else snap immediately to that location
        else
        {
            Debug.Log($"Snapping object {Id} from {transform.position} to {location}");
            transform.position = location;
            Target = location;
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
