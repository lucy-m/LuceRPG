using LuceRPG.Game.WorldObjects;
using LuceRPG.Models;
using Microsoft.FSharp.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LuceRPG.Game.Overlords
{
    public class InteractionOverlord : MonoBehaviour
    {
        public GameObject SpeechBubblePrefab = null;
        public Canvas WorldTextCanvas = null;

        private IList<SpeechBubbleController> previousSpeechBubbles = null;

        private void Start()
        {
            Registry.Streams.Interactions.RegisterOnInteract(InteractWith);
        }

        public void InteractWith(string objId, Vector3 position, WithId.Model<FSharpList<InteractionModule.One>> interaction)
        {
            Debug.Log($"Running interaction {interaction.id}");

            if (previousSpeechBubbles != null)
            {
                foreach (var c in previousSpeechBubbles)
                {
                    if (c != null)
                    {
                        Destroy(c.gameObject);

                    }
                }
            }

            previousSpeechBubbles = new List<SpeechBubbleController>();

            foreach (var iOne in interaction.value)
            {
                var text = iOne.Item;
                var speechBubble = Instantiate(SpeechBubblePrefab, position, Quaternion.identity, WorldTextCanvas.transform);
                var sbc = speechBubble.GetComponent<SpeechBubbleController>();
                sbc.ShowText(text);
                previousSpeechBubbles.Add(sbc);
            }
        }
    }
}
