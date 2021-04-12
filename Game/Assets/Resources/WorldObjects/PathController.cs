using LuceRPG.Game;
using LuceRPG.Models;
using Microsoft.FSharp.Collections;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class PathController : MonoBehaviour
{
    public Sprite SpriteStraight;
    public Sprite SpriteEnd;
    public Sprite Sprite4way;
    public Sprite SpriteL;
    public Sprite SpriteT;
    public Sprite SpriteSingle;

    public SpriteRenderer SpriteRenderer;
    public PointModule.Model Point;

    [SerializeField] private bool _northNeighbour;
    [SerializeField] private bool _southNeighbour;
    [SerializeField] private bool _eastNeighbour;
    [SerializeField] private bool _westNeighbour;

    private FSharpSet<PointModule.Model> Paths => Registry.Stores.World.Paths;

    private void Start()
    {
        _northNeighbour = HasNeighbour(DirectionModule.Model.North);
        _southNeighbour = HasNeighbour(DirectionModule.Model.South);
        _eastNeighbour = HasNeighbour(DirectionModule.Model.East);
        _westNeighbour = HasNeighbour(DirectionModule.Model.West);

        SetSprite();
    }

    private bool HasNeighbour(DirectionModule.Model dir)
    {
        if (Point != null)
        {
            var pd = DirectionModule.movePoint(dir, 1, Point);

            return SetModule.Contains(pd, Paths);
        }
        else
        {
            return false;
        }
    }

    private void SetSpriteRotation(float z)
    {
        SpriteRenderer.transform.rotation = Quaternion.Euler(0, 0, z);
    }

    private void SetSprite()
    {
        var neighbourCount =
            new bool[] { _northNeighbour, _southNeighbour, _eastNeighbour, _westNeighbour }
            .Count(b => b);

        if (neighbourCount == 0)
        {
            SpriteRenderer.sprite = SpriteSingle;
        }
        else if (neighbourCount == 1)
        {
            // Sprite faces to east
            SpriteRenderer.sprite = SpriteEnd;

            if (_northNeighbour)
            {
                SetSpriteRotation(-90);
            }
            else if (_southNeighbour)
            {
                SetSpriteRotation(90);
            }
            else if (_eastNeighbour)
            {
                SetSpriteRotation(180);
            }
            else if (_westNeighbour)
            {
                SetSpriteRotation(0);
            }
        }
        else if (neighbourCount == 2)
        {
            if (_northNeighbour)
            {
                if (_southNeighbour)
                {
                    SpriteRenderer.sprite = SpriteStraight;
                    SetSpriteRotation(90);
                }
                else if (_eastNeighbour)
                {
                    SpriteRenderer.sprite = SpriteL;
                    SetSpriteRotation(180);
                }
                else if (_westNeighbour)
                {
                    SpriteRenderer.sprite = SpriteL;
                    SetSpriteRotation(-90);
                }
            }
            else if (_southNeighbour)
            {
                if (_eastNeighbour)
                {
                    SpriteRenderer.sprite = SpriteL;
                    SetSpriteRotation(90);
                }
                else if (_westNeighbour)
                {
                    SpriteRenderer.sprite = SpriteL;
                    SetSpriteRotation(0);
                }
            }
            else if (_eastNeighbour)
            {
                if (_westNeighbour)
                {
                    SpriteRenderer.sprite = SpriteStraight;
                    SetSpriteRotation(0);
                }
            }
        }
        else if (neighbourCount == 3)
        {
            SpriteRenderer.sprite = SpriteT;

            if (!_northNeighbour)
            {
                SetSpriteRotation(0);
            }
            else if (!_southNeighbour)
            {
                SetSpriteRotation(180);
            }
            else if (!_eastNeighbour)
            {
                SetSpriteRotation(-90);
            }
            else if (!_westNeighbour)
            {
                SetSpriteRotation(90);
            }
        }
        else
        {
            SpriteRenderer.sprite = Sprite4way;
        }
    }

    private void Update()
    {
        if (Application.isEditor)
        {
            SetSprite();
        }
    }
}
