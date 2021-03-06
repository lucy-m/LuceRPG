using LuceRPG.Game.Editor;
using LuceRPG.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class DirectionalSpriteController : MonoBehaviour
{
    public GameObject NorthSprite;
    public GameObject SouthSprite;
    public GameObject EastSprite;
    public GameObject WestSprite;

    public bool NorthUseSouthXFlipped = false;
    public bool NorthUseSouthYFlipped = false;
    public bool NorthUseSouthZFlipped = false;

    public bool WestUseEastFlipped = false;

    public float SpriteHeight = 0.0f;
    public float SpriteWidth = 0.0f;

    private Dictionary<DirectionModule.Model, GameObject> _sprites;

    // Used for inspector
    [SerializeField]
    private DirectionInput _testDirection = DirectionInput.South;

    private DirectionModule.Model _direction;

    public DirectionModule.Model Direction
    {
        get => _direction;
        set
        {
            if (_direction != value && value != null)
            {
                _direction = value;
                _testDirection = value.ToInput();
                SetSprites();
            }
        }
    }

    private void Awake()
    {
        _sprites = new Dictionary<DirectionModule.Model, GameObject>();

        _sprites[DirectionModule.Model.North] = NorthSprite;
        _sprites[DirectionModule.Model.South] = SouthSprite;
        _sprites[DirectionModule.Model.East] = EastSprite;
        _sprites[DirectionModule.Model.West] = WestSprite;
    }

    private void SetSprites()
    {
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.one;

        // Always refresh the sprites dict in editor mode
        if (Application.isEditor)
        {
            _sprites = new Dictionary<DirectionModule.Model, GameObject>();

            _sprites[DirectionModule.Model.North] = NorthSprite;
            _sprites[DirectionModule.Model.South] = SouthSprite;
            _sprites[DirectionModule.Model.East] = EastSprite;
            _sprites[DirectionModule.Model.West] = WestSprite;
        }

        var nonMatchingSprites = _sprites.Where(kvp => kvp.Key != _direction && kvp.Value != null);

        foreach (var s in nonMatchingSprites)
        {
            s.Value.SetActive(false);
        }

        var matchingSprite = _sprites[_direction];
        if (matchingSprite != null)
        {
            matchingSprite.SetActive(true);
        }

        if (NorthUseSouthXFlipped || NorthUseSouthZFlipped || NorthUseSouthYFlipped)
        {
            if (_direction.IsNorth && SouthSprite != null)
            {
                var xPos = NorthUseSouthXFlipped ? SpriteWidth : 0;
                var xScale = NorthUseSouthXFlipped ? -1 : 1;

                var yPos = NorthUseSouthYFlipped ? SpriteHeight : 0;
                var yScale = NorthUseSouthYFlipped ? -1 : 1;

                var zPos = NorthUseSouthZFlipped ? 1 : 0;
                var zScale = NorthUseSouthZFlipped ? -1 : 1;

                SouthSprite.SetActive(true);
                transform.localPosition = new Vector3(xPos, yPos, zPos);
                transform.localScale = new Vector3(xScale, yScale, zScale);
            }
        }

        if (WestUseEastFlipped)
        {
            if (_direction.IsWest && EastSprite != null)
            {
                EastSprite.SetActive(true);
                transform.localPosition = new Vector3(SpriteWidth, 0, 0);
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }
    }

    private void Update()
    {
        // Allows changes to be previewed in Edit Mode
        if (!Application.isPlaying)
        {
            Direction = _testDirection.ToModel();
        }
    }
}
