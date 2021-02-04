using UnityEngine;
using UnityEngine.UI;

public class SpeechBubbleController : MonoBehaviour
{
    public Text Text;
    public GameObject ShowMoreIndicator;

    private string remaining = null;

    public void ShowText(string text)
    {
        Text.text = text;
        Canvas.ForceUpdateCanvases();
        var ccv = Text.cachedTextGenerator.characterCountVisible;

        var hasOverflow = ccv != text.Length;

        remaining = hasOverflow ? text.Substring(ccv) : null;
        ShowMoreIndicator.SetActive(hasOverflow);
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
