using LuceRPG.Game.Util;
using LuceRPG.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UniversalController : MonoBehaviour
{
    public readonly static Dictionary<string, UniversalController> Controllers
        = new Dictionary<string, UniversalController>();

    private const string UnitNameTag = "UnitName";

    private string _id = "";
    private WorldObjectModule.Payload _model;
    private float _speed = 0;

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

    public WorldObjectModule.Payload Model
    {
        get => _model;
        set
        {
            _model = value;

            var travelTime = _model == null ? 0 :
                (WorldObjectModule.travelTime(_model) / System.TimeSpan.TicksPerMillisecond);
            _speed = travelTime == 0 ? 0 : 1000.0f / travelTime;
            Debug.Log($"Setting speed for {Id} to {_speed}, travel time {travelTime}");

            if (_model.t.IsPlayer)
            {
                var asPlayer = ((WorldObjectModule.TypeModule.Model.Player)_model.t).Item;
                var name = asPlayer.name;

                var unitNameGo =
                    gameObject
                        .GetComponentsInChildren<TextMesh>()
                        .FirstOrDefault(tm => tm.gameObject.CompareTag(UnitNameTag));

                if (unitNameGo == null)
                {
                    Debug.LogWarning($"Could not set player name  {name} as no unit name component found");
                }
                else
                {
                    unitNameGo.text = name;
                }
            }
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
            var offset = DirectionUtil.AsVector3(moved.Item2);

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
        if (distance < _speed)
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
        var newPosition = Vector3.MoveTowards(transform.position, Target, _speed * Time.deltaTime);
        transform.position = newPosition;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawCube(Target, Vector3.one);
    }
}
