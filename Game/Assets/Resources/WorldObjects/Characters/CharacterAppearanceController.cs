using LuceRPG.Game.Editor;
using LuceRPG.Game.Utility;
using LuceRPG.Models;
using System;
using UnityEngine;

[ExecuteAlways]
public class CharacterAppearanceController : MonoBehaviour
{
    public GameObject HairLong;
    public GameObject HairShort;
    public SpriteColourController Skin;
    public SpriteColourController Top;
    public SpriteColourController Bottoms;

    private CharacterDataModule.Model _characterData;
    private GameObject _currentHair;

    // Used for inspector

    [SerializeField] private HairStyleInput _hairStyle = HairStyleInput.Egg;
    [SerializeField] private Color _hairColour = Color.white;
    [SerializeField] private Color _skinColour = Color.white;
    [SerializeField] private Color _topColour = Color.white;
    [SerializeField] private Color _btmColour = Color.white;

    public CharacterDataModule.Model CharacterData
    {
        get => _characterData;
        set
        {
            _characterData = value;

            if (_currentHair != null)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(_currentHair);
                }
                else
                {
                    Destroy(_currentHair);
                }
            }

            var newHair = GetHair(_characterData.hairStyle);

            if (newHair != null)
            {
                _currentHair = Instantiate(newHair, transform);

                var scc = _currentHair.GetComponent<SpriteColourController>();

                if (scc != null)
                {
                    var colour = _characterData.hairColour.ToColor();
                    scc.Colour = colour;
                }
            }

            if (Skin != null)
            {
                Skin.Colour = _characterData.skinColour.ToColor();
            }

            if (Top != null)
            {
                Top.Colour = _characterData.topColour.ToColor();
            }

            if (Bottoms != null)
            {
                Bottoms.Colour = _characterData.bottomColour.ToColor();
            }
        }
    }

    private GameObject GetHair(CharacterDataModule.HairStyle hairStyle)
    {
        if (hairStyle.IsLong)
        {
            return HairLong;
        }
        else if (hairStyle.IsShort)
        {
            return HairShort;
        }

        return null;
    }

    private void Update()
    {
        // Allows changes to be previewed in Edit Mode
        if (!Application.isPlaying)
        {
            var hairStyle = _hairStyle switch
            {
                HairStyleInput.Long => CharacterDataModule.HairStyle.Long,
                HairStyleInput.Short => CharacterDataModule.HairStyle.Short,
                _ => CharacterDataModule.HairStyle.Egg
            };

            var hairColour = _hairColour.ToByteTuple();
            var skinColour = _skinColour.ToByteTuple();
            var topColour = _topColour.ToByteTuple();
            var bottomColour = _btmColour.ToByteTuple();

            var charData = new CharacterDataModule.Model(
                "",
                hairStyle,
                hairColour,
                skinColour,
                topColour,
                bottomColour
            );

            CharacterData = charData;
        }
    }
}
