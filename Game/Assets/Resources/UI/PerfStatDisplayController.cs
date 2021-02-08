using LuceRPG.Game.Stores;
using LuceRPG.Game.Util;
using UnityEngine;
using UnityEngine.UI;

namespace LuceRPG.Game.UI
{
    public class PerfStatDisplayController : MonoBehaviour
    {
        private Text text;
        private PerfStatsStore PerfStats => Registry.Stores.PerfStats;

        private void Start()
        {
            text = GetComponent<Text>();
            StartCoroutine(CoroutineUtil.RunForever(UpdateText, 1));
        }

        private void UpdateText()
        {
            if (text != null)
            {
                var ping = $"Ping: {PerfStats.Ping} ms";
                text.text = ping;
            }
        }
    }
}
