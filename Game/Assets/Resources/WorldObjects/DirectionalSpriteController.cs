using LuceRPG.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DirectionalSpriteController : MonoBehaviour
{
    public GameObject NorthSprite;
    public GameObject SouthSprite;
    public GameObject EastSprite;
    public GameObject WestSprite;

    public bool NorthUseSouthFlipped = false;

    private Dictionary<DirectionModule.Model, GameObject> _sprites;
    private DirectionModule.Model _lastDirection;

    private void Awake()
    {
        _sprites = new Dictionary<DirectionModule.Model, GameObject>();

        _sprites[DirectionModule.Model.North] = NorthSprite;
        _sprites[DirectionModule.Model.South] = SouthSprite;
        _sprites[DirectionModule.Model.East] = EastSprite;
        _sprites[DirectionModule.Model.West] = WestSprite;
    }

    public void SetDirection(DirectionModule.Model direction)
    {
        Debug.Log($"Directional sprite setting to {direction}");

        if (_lastDirection != direction && direction != null)
        {
            var nonMatchingSprites = _sprites.Where(kvp => kvp.Key != direction && kvp.Value != null);

            foreach (var s in nonMatchingSprites)
            {
                s.Value.SetActive(false);
            }

            var matchingSprite = _sprites[direction];
            if (matchingSprite != null)
            {
                matchingSprite.SetActive(true);
            }

            if (NorthUseSouthFlipped && SouthSprite != null)
            {
                if (direction == DirectionModule.Model.North)
                {
                    SouthSprite.SetActive(true);
                    SouthSprite.transform.localPosition = new Vector3(2, 0, 1);
                    SouthSprite.transform.localScale = new Vector3(-1, 1, -1);
                }
                else if (direction == DirectionModule.Model.South)
                {
                    SouthSprite.transform.localPosition = Vector3.zero;
                    SouthSprite.transform.localScale = Vector3.one;
                }
            }

            _lastDirection = direction;
        }
    }
}
