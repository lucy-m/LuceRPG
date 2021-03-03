using LuceRPG.Game.Utility;
using LuceRPG.Models;
using UnityEngine;

public class CharacterAppearanceController : MonoBehaviour
{
    public GameObject HairLong;
    public GameObject HairShort;
    public SpriteColourController Skin;
    public SpriteColourController Top;
    public SpriteColourController Bottoms;

    private CharacterDataModule.Model _characterData;
    private GameObject _currentHair;

    public CharacterDataModule.Model CharacterData
    {
        get => _characterData;
        set
        {
            _characterData = value;

            if (_currentHair != null)
            {
                Destroy(_currentHair);
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
        return HairLong;

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
}
