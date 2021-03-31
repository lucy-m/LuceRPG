using LuceRPG.Game;
using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using System;
using UnityEngine;

public class CursorDisplayController : MonoBehaviour
{
    public GameObject CornerPrefab;

    private PointModule.Model _position = PointModule.create(0, 0);
    private PointModule.Model _size = PointModule.create(1, 1);

    private GameObject BottomLeft;
    private GameObject BottomRight;
    private GameObject TopLeft;
    private GameObject TopRight;

    public PointModule.Model Position
    {
        get => _position;
        set
        {
            _position = value;

            var objectAtPosition = MapModule.TryFind(_position, Registry.Stores.World.World.blocked);

            if (objectAtPosition.HasValue() && objectAtPosition.Value.IsObject)
            {
                var obj = (objectAtPosition.Value as WorldModule.BlockedType.Object).Item;
                var size = WorldObjectModule.size(obj.value);
                var boundedSize = PointModule.create(Math.Max(1, size.x), Math.Max(1, size.y));
                _position = WorldObjectModule.btmLeft(obj.value);

                Size = boundedSize;
            }
            else
            {
                Size = PointModule.p1x1;
            }

            transform.position = new Vector3(_position.x, _position.y, _position.y);
        }
    }

    public PointModule.Model Size
    {
        get => _size;
        set
        {
            _size = value;
            SetSpritePositions();
        }
    }

    private void Awake()
    {
        BottomLeft = Instantiate(CornerPrefab, transform);
        BottomRight = Instantiate(CornerPrefab, transform);
        TopLeft = Instantiate(CornerPrefab, transform);
        TopRight = Instantiate(CornerPrefab, transform);

        BottomLeft.transform.rotation = Quaternion.identity;
        BottomRight.transform.rotation = Quaternion.Euler(0, 0, 90);
        TopLeft.transform.rotation = Quaternion.Euler(0, 0, -90);
        TopRight.transform.rotation = Quaternion.Euler(0, 0, 180);

        SetSpritePositions();
    }

    private void SetSpritePositions()
    {
        BottomLeft.transform.localPosition = new Vector3(0, 0, -0.01f);
        BottomRight.transform.localPosition = new Vector3(_size.x, 0, -0.01f);
        TopLeft.transform.localPosition = new Vector3(0, _size.y, 0.99f);
        TopRight.transform.localPosition = new Vector3(_size.x, _size.y, 0.99f);
    }
}
