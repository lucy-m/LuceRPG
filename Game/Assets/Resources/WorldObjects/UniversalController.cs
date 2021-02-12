using LuceRPG.Game.Utility;
using LuceRPG.Models;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LuceRPG.Game.WorldObjects
{
    public class UniversalController : MonoBehaviour
    {
        public readonly static Dictionary<string, UniversalController> Controllers
            = new Dictionary<string, UniversalController>();

        private Vector3 _target;

        private string _id = "";
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

        public void SetModelProps(WorldObjectModule.Payload model)
        {
            var travelTime = model == null ? 0 :
                (WorldObjectModule.travelTime(model) / TimeSpan.TicksPerMillisecond);
            _speed = travelTime == 0 ? 0 : 1050.0f / travelTime;
        }

        public Vector3 Target
        {
            get => _target;
            set
            {
                var z = value.y;
                _target = new Vector3(value.x, value.y, z);
                transform.position = new Vector3(transform.position.x, transform.position.y, z);
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
                var offset = moved.Item2.AsVector3();

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
            if (distance <= _speed)
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

        public void OnMouseDown()
        {
            Registry.Streams.Interactions.Next(Id, transform.position);
        }
    }
}
