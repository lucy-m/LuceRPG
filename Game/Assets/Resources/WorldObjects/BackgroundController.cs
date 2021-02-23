using LuceRPG.Models;
using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    public SpriteRenderer SpriteRenderer;

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

    private RectModule.Model _rect;
}
