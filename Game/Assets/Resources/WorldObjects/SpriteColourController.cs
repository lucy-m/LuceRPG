using UnityEngine;

[ExecuteAlways]
public class SpriteColourController : MonoBehaviour
{
    public SpriteRenderer[] Sprites;
    public SpriteColourController[] Children;

    [SerializeField]
    private Color _colour = Color.white;

    public Color Colour
    {
        get => _colour;
        set
        {
            _colour = value;
            SetSpriteColour();
        }
    }

    private void Start()
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

        if (Children != null && Colour != null)
        {
            foreach (var c in Children)
            {
                c.Colour = Colour;
            }
        }
    }

    private void Update()
    {
        // Allows changes to be made in edit mode
        if (Application.isEditor)
        {
            SetSpriteColour();
        }
    }
}
