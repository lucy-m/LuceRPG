using System;
using System.IO;
using UnityEngine;

[Serializable]
public class Config
{
    public string BaseUrl;
    public string Username;
    public string Password;
}

public interface IConfigLoader
{
    Config Config { get; }

    void SaveConfig();
}

public class ConfigLoader : IConfigLoader
{
    public Config Config { get; private set; }

    private readonly string _configPath;

    public ConfigLoader(string configPath)
    {
        _configPath = configPath;

        if (!File.Exists(configPath))
        {
            Debug.Log("Creating new config file");
            Config = new Config
            {
                BaseUrl = "http://localhost:5000/",
                Username = "???",
                Password = "???"
            };
            SaveConfig();
        }
        else
        {
            Debug.Log("Reading existing config file");
            var asString = File.ReadAllText(configPath);
            Config = JsonUtility.FromJson<Config>(asString);
        }
    }

    public void SaveConfig()
    {
        var asString = JsonUtility.ToJson(Config, true);
        File.WriteAllText(_configPath, asString);
    }
}
