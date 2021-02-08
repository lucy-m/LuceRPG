using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpeechBubbleController : MonoBehaviour
{
    public Text Text;
    public GameObject ShowMoreIndicator;

    private string remaining = null;

    private string playerName = "Player";

    private void Awake()
    {
        var playerId = Registry.WorldStore.PlayerId;
        var world = Registry.WorldStore.World;

        if (playerId != null && world != null)
        {
            var playerObj = MapModule.TryFind(playerId, world.objects);

            if (playerObj.HasValue())
            {
                playerName = WorldObjectModule.getName(playerObj.Value.value);
            }
        }
    }

    public void ShowText(string text)
    {
        var replaced = text.Replace("{player}", playerName);
        ShowTextWithoutReplacement(replaced);
    }

    private void ShowTextWithoutReplacement(string text)
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
            ShowTextWithoutReplacement(remaining);
        }
    }
}
