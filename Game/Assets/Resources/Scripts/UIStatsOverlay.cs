using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIStatsOverlay : MonoBehaviour
{
    public static UIStatsOverlay Instance = null;

    public Text PingText = null;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(GetComponent<UIStatsOverlay>());
        }
        else
        {
            Instance = this;
        }
    }

    public void SetPingMs(int ms)
    {
        PingText.text = $"Ping {ms} ms";
    }
}
