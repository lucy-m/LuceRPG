using UnityEngine;

public class SpriteColourController : MonoBehaviour
{
    public SpriteRenderer[] Sprites;

    private Color _colour;

    public Color Colour
    {
        get => _colour;
        set
        {
            _colour = value;
            SetSpriteColour();
        }
    }

    private void Awake()
    {
        SetSpriteColour();
    }

    private void SetSpriteColour()
    {
        if (Sprites != null && Colour != null)
        {
            foreach (var s in Sprites)
            {
                s.color = Colour;
            }
        }
    }
}
