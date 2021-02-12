using UnityEngine;

public class SkinToneController : MonoBehaviour
{
    public SpriteRenderer[] SkinSprites;
    public Color Color;

    private void Awake()
    {
        if (SkinSprites != null && Color != null)
        {
            foreach (var s in SkinSprites)
            {
                s.color = Color;
            }
        }
    }
}
