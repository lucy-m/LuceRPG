using System.IO;
using UnityEngine;
using LuceRPG.Game.Models;

namespace LuceRPG.Game.Services
{
    public class ConfigLoaderService
    {
        private readonly string ConfigPath = "credentials.json";

        public void LoadConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                Debug.Log("Creating new config file");
                Registry.Stores.Config.Config = Config.Default;
                SaveConfig();
            }
            else
            {
                Debug.Log("Reading existing config file");
                var asString = File.ReadAllText(ConfigPath);
                Registry.Stores.Config.Config = JsonUtility.FromJson<Config>(asString);
            }
        }

        public void SaveConfig()
        {
            var config = Registry.Stores.Config;
            var asString = JsonUtility.ToJson(config, true);
            File.WriteAllText(ConfigPath, asString);
        }
    }
}
