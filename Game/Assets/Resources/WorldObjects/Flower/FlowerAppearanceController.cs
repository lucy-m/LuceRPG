using LuceRPG.Game.Utility;
using LuceRPG.Models;
using System.Linq;
using UnityEngine;

public class FlowerAppearanceController : MonoBehaviour
{
    public GameObject[] Stems;
    public GameObject[] Heads;
    public SpriteColourController HeadColourer;

    private FlowerModule.Model _model;

    public FlowerModule.Model Model
    {
        get => _model;
        set
        {
            _model = value;
            HeadColourer.Colour = _model.headColour.ToColor();

            var stemIndex = _model.stem >= Stems.Length ? 0 : _model.stem;
            for (var i = 0; i < Stems.Length; i++)
            {
                var stem = Stems[i];
                var active = i == stemIndex;

                stem.SetActive(active);
            }

            var headIndex = _model.head >= Heads.Length ? 0 : _model.head;
            for (var i = 0; i < Heads.Length; i++)
            {
                var head = Heads[i];
                var active = i == headIndex;
                head.SetActive(active);
            }
        }
    }
}
