using System;
using System.IO;
using UnityEngine;

[Serializable]
public class Config
{
    public string BaseUrl;
}

public class ConfigLoader : MonoBehaviour
{
    public static ConfigLoader Instance = null;
    private const string ConfigPath = "config.json";

    public Config Config { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(GetComponent<ConfigLoader>());
        }
        else
        {
            Instance = this;

            if (!File.Exists(ConfigPath))
            {
                Debug.Log("Creating new config file");
                Config = new Config { BaseUrl = "http://localhost:5000/" };
                var asString = JsonUtility.ToJson(Config, true);
                File.WriteAllText(ConfigPath, asString);
            }
            else
            {
                Debug.Log("Reading existing config file");
                var asString = File.ReadAllText(ConfigPath);
                Config = JsonUtility.FromJson<Config>(asString);
            }
        }
    }
}
