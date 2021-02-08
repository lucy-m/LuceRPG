using System;

namespace LuceRPG.Game.Models
{
    [Serializable]
    public class Config
    {
        public string BaseUrl;
        public string Username;
        public string Password;

        public static Config Default = new Config()
        {
            BaseUrl = "http://localhost:5000/",
            Username = "???",
            Password = "???",
        };
    }
}
