using UnityEngine;
using UnityEngine.UI;

public class SpeechBubbleController : MonoBehaviour
{
    public Text Text;
    public float LiveTimer = 4.0f;

    private string remaining = null;

    public void ShowText(string text)
    {
        Text.text = text;
        Destroy(gameObject, LiveTimer);
        Canvas.ForceUpdateCanvases();
        var c = Text.cachedTextGenerator.characterCountVisible;

        //remaining = c == text.Length ? null : text.Substring(c);
    }

    private void OnMouseDown()
    {
        Debug.Log("Clicked on speech bubble");
    }
}
