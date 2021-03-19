using LuceRPG.Game.Utility;
using LuceRPG.Models;
using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    public SpriteRenderer SpriteRenderer;
    public Sprite GrassSprite;
    public Sprite PlanksSprite;

    private RectModule.Model _rect;
    private WorldBackgroundModule.Model _bg;

    public RectModule.Model Rect
    {
        get => _rect;
        set
        {
            _rect = value;

            var size = new Vector2(_rect.size.x, _rect.size.y);
            var pos = size / 2;
            SpriteRenderer.size = size;
            SpriteRenderer.transform.localPosition = pos;
        }
    }

    public WorldBackgroundModule.Model Bg
    {
        get => _bg;
        set
        {
            _bg = value;

            if (_bg.t.IsGrass)
            {
                SpriteRenderer.sprite = GrassSprite;
            }
            else if (_bg.t.IsPlanks)
            {
                SpriteRenderer.sprite = PlanksSprite;
            }

            var colour = _bg.colour.ToColor();
            SpriteRenderer.color = colour;
        }
    }
}
