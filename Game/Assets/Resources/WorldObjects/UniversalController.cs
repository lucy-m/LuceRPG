using LuceRPG.Game.Utility;
using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
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
        private WorldObjectModule.Payload _lastModel;
        private DirectionalSpriteRoot _directionalSpriteRoot;

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

        public void SetModelProps(WorldObjectModule.Payload model)
        {
            var travelTime = model == null ? 0 :
                (WorldObjectModule.travelTime(model) / TimeSpan.TicksPerMillisecond);
            _speed = travelTime == 0 ? 0 : 1050.0f / travelTime;

            if (model.t.IsWarp)
            {
                var warp = model.t as WorldObjectModule.TypeModule.Model.Warp;
                var wac = GetComponent<WarpAppearanceController>();
                if (wac != null)
                {
                    wac.Appearance = warp.Item.appearance;
                }
            }
            else if (model.t.IsFlower)
            {
                var flower = (model.t as WorldObjectModule.TypeModule.Model.Flower).Item;
                var fac = GetComponent<FlowerAppearanceController>();
                if (fac != null)
                {
                    fac.Model = flower;
                }
            }
        }

        private void Awake()
        {
            _directionalSpriteRoot = GetComponent<DirectionalSpriteRoot>();
        }

        private void Start()
        {
            Target = transform.position;
        }

        private void OnDestroy()
        {
            Controllers.Remove(_id);
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
            var model = GetModel();

            if (model == null)
            {
                Destroy(gameObject);
            }
            else
            {
                if (_lastModel == null || _lastModel.btmLeft != model.btmLeft)
                {
                    Target = model.btmLeft.ToVector3();
                }

                if (_directionalSpriteRoot != null)
                {
                    _directionalSpriteRoot.Direction = model.facing;
                }

                var newPosition = Vector3.MoveTowards(transform.position, Target, _speed * Time.deltaTime);
                transform.position = newPosition;

                _lastModel = model;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawCube(Target, Vector3.one);
        }

        public void OnMouseDown()
        {
            Registry.Streams.Interactions.Next(Id, transform.position);
        }

        public WorldObjectModule.Payload GetModel()
        {
            var worldStore = Registry.Stores.World.World;
            var tModel = MapModule.TryFind(Id, worldStore.objects);

            if (tModel.HasValue())
            {
                return tModel.Value.value;
            }
            else
            {
                return null;
            }
        }
    }
}
