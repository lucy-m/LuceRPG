using LuceRPG.Game;
using LuceRPG.Game.Stores;
using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using System;
using UnityEngine;

public class CursorDisplayController : MonoBehaviour
{
    public GameObject CornerPrefab;

    public SpriteColourController ColourController { get; private set; }

    public static readonly Color OverObjectColor = new Color(0.9f, 0.9f, 0.6f);
    public static readonly Color NoObjectColour = new Color(0.6f, 0.9f, 0.9f);

    private string _cursorOverObjectId;
    private PointModule.Model _position = PointModule.create(0, 0);
    private PointModule.Model _size = PointModule.create(1, 1);

    private GameObject BottomLeft;
    private GameObject BottomRight;
    private GameObject TopLeft;
    private GameObject TopRight;

    private CursorStore CursorStore => Registry.Stores.Cursor;

    public PointModule.Model Position
    {
        get => _position;
        set
        {
            if (_position != value && value != null)
            {
                _position = value;
                transform.position = new Vector3(_position.x, _position.y, _position.y);
            }
        }
    }

    public PointModule.Model Size
    {
        get => _size;
        set
        {
            if (_size != value)
            {
                _size = value;
                SetSpritePositions();
            }
        }
    }

    private void Awake()
    {
        BottomLeft = Instantiate(CornerPrefab, transform);
        BottomRight = Instantiate(CornerPrefab, transform);
        TopLeft = Instantiate(CornerPrefab, transform);
        TopRight = Instantiate(CornerPrefab, transform);

        ColourController = gameObject.AddComponent<SpriteColourController>();
        ColourController.Children = new SpriteColourController[]{
            BottomLeft.GetComponent<SpriteColourController>(),
            BottomRight.GetComponent<SpriteColourController>(),
            TopLeft.GetComponent<SpriteColourController>(),
            TopRight.GetComponent<SpriteColourController>()
        };

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

    private void Update()
    {
        if (CursorStore.CursorOverObject == null)
        {
            Position = CursorStore.Position;
            Size = PointModule.p1x1;
            ColourController.Colour = NoObjectColour;
        }
        else
        {
            if (CursorStore.CursorOverObject.id != _cursorOverObjectId)
            {
                var position = WorldObjectModule.btmLeft(CursorStore.CursorOverObject.value);
                var size = WorldObjectModule.size(CursorStore.CursorOverObject.value);
                Position = position;
                Size = size;
                ColourController.Colour = OverObjectColor;
            }
        }

        _cursorOverObjectId = CursorStore.CursorOverObject?.id;
    }
}
