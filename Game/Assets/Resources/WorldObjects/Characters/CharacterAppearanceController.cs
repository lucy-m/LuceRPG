using LuceRPG.Game.Utility;
using LuceRPG.Models;
using UnityEngine;

public class CharacterAppearanceController : MonoBehaviour
{
    public GameObject HairLong;
    public GameObject HairShort;

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
}
