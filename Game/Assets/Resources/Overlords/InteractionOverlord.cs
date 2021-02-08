using LuceRPG.Models;
using Microsoft.FSharp.Collections;
using UnityEngine;

namespace LuceRPG.Game.Overlords
{
    public class InteractionOverlord : MonoBehaviour
    {
        public GameObject SpeechBubblePrefab = null;
        public Canvas WorldTextCanvas = null;

        private void Start()
        {
            Registry.Streams.Interactions.RegisterOnInteract(InteractWith);
        }

        public void InteractWith(string objId, Vector3 position, WithId.Model<FSharpList<InteractionModule.One>> interaction)
        {
            Debug.Log($"Running interaction {interaction.id}");

            foreach (var iOne in interaction.value)
            {
                var text = iOne.Item;
                var speechBubble = Instantiate(SpeechBubblePrefab, position, Quaternion.identity, WorldTextCanvas.transform);
                var sbc = speechBubble.GetComponent<SpeechBubbleController>();
                sbc.ShowText(text);
            }
        }
    }
}
