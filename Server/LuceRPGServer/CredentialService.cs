using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace LuceRPG.Server
{
    public class CredentialService
    {
        private class Credential
        {
            public string? Username { get; set; }
            public string? Password { get; set; }
        }

        private readonly ILogger<CredentialService> _logger;
        private readonly Dictionary<string, string> Credentials;

        public CredentialService(ILogger<CredentialService> logger)
        {
            _logger = logger;

            Credentials = new Dictionary<string, string>();

            using var stream = new StreamReader("credentials.json");
            var json = stream.ReadToEnd();
            var creds = JsonConvert.DeserializeObject<List<Credential>>(json);

            foreach (var c in creds)
            {
                if (c.Username != null && c.Password != null)
                {
                    Credentials[c.Username] = c.Password;
                }
            }

            _logger.LogInformation($"Loaded {Credentials.Count} credentials");
        }

        public bool IsValid(string username, string password)
        {
            if (Credentials.TryGetValue(username, out var storedPw))
            {
                return storedPw == password;
            }
            else
            {
                return false;
            }
        }
    }
}
