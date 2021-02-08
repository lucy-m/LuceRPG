using System.IO;
using UnityEngine;
using LuceRPG.Game.Models;

namespace LuceRPG.Game.Services
{
    public class ConfigLoaderService
    {
        private string Path => Registry.Stores.Config.Path;

        public void LoadConfig()
        {
            if (!File.Exists(Path))
            {
                Debug.Log("Creating new config file");
                Registry.Stores.Config.Config = Config.Default;
                SaveConfig();
            }
            else
            {
                Debug.Log("Reading existing config file");
                var asString = File.ReadAllText(Path);
                var c = JsonUtility.FromJson<Config>(asString);
                Registry.Stores.Config.Config = c;
            }
        }

        public void SaveConfig()
        {
            var config = Registry.Stores.Config.Config;
            var asString = JsonUtility.ToJson(config, true);
            File.WriteAllText(Path, asString);
        }
    }
}
