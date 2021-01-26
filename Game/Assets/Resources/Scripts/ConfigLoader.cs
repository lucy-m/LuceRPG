using System;
using System.IO;
using UnityEngine;

[Serializable]
public class Config
{
    public string BaseUrl;
}

public interface IConfigLoader
{
    Config Config { get; }
}

public class ConfigLoader : IConfigLoader
{
    public Config Config { get; private set; }

    public ConfigLoader(string configPath)
    {
        if (!File.Exists(configPath))
        {
            Debug.Log("Creating new config file");
            Config = new Config { BaseUrl = "http://localhost:5000/" };
            var asString = JsonUtility.ToJson(Config, true);
            File.WriteAllText(configPath, asString);
        }
        else
        {
            Debug.Log("Reading existing config file");
            var asString = File.ReadAllText(configPath);
            Config = JsonUtility.FromJson<Config>(asString);
        }
    }
}
