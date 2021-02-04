using UnityEngine;
using UnityEngine.UI;

public class SpeechBubbleController : MonoBehaviour
{
    public Text Text;

    private string remaining = null;

    public void ShowText(string text)
    {
        Text.text = text;
        Canvas.ForceUpdateCanvases();
        var c = Text.cachedTextGenerator.characterCountVisible;

        remaining = c == text.Length ? null : text.Substring(c);
    }

    private void OnMouseDown()
    {
        if (remaining == null)
        {
            Destroy(gameObject);
        }
        else
        {
            ShowText(remaining);
        }
    }
}
