using System;

namespace LuceRPG.Game.Models
{
    [Serializable]
    public class Config
    {
        public string BaseUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public static Config Default = new Config()
        {
            BaseUrl = "http://localhost:5000/",
            Username = "???",
            Password = "???",
        };
    }
}
