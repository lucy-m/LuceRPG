using LuceRPG.Models;
using LuceRPG.Utility;
using UnityEngine;

public class InteractionController : MonoBehaviour
{
    public static InteractionController Instance = null;

    public GameObject SpeechBubblePrefab = null;
    public Canvas WorldTextCanvas = null;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(GetComponent<InteractionController>());
        }
        else
        {
            Instance = this;
        }
    }

    public void InteractWith(string objId, Vector3 position)
    {
        var interaction = WorldModule.getInteraction(
            objId,
            Registry.WorldStore.Interactions.Value,
            Registry.WorldStore.World
        );

        if (interaction.HasValue())
        {
            Debug.Log($"Running interaction {interaction.Value.id}");

            foreach (var iOne in interaction.Value.value)
            {
                var text = iOne.Item;
                var speechBubble = Instantiate(SpeechBubblePrefab, position, Quaternion.identity, WorldTextCanvas.transform);
                var sbc = speechBubble.GetComponent<SpeechBubbleController>();
                sbc.ShowText(text);
            }
        }
    }
}
