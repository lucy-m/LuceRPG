using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LuceRPG.Server
{
    public interface ICredentialService
    {
        bool IsValid(string username, string password);

        string AddOrResetUser(string username);
    }

    public class CredentialService : ICredentialService
    {
        private class Credential
        {
            public string? Username { get; set; }
            public string? Password { get; set; }
        }

        private readonly string path = "credentials.json";
        private readonly ILogger<CredentialService> _logger;
        private readonly Dictionary<string, string> Credentials;

        public CredentialService(ILogger<CredentialService> logger)
        {
            _logger = logger;

            Credentials = new Dictionary<string, string>();

            var json = File.ReadAllText(path);
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

        public string AddOrResetUser(string username)
        {
            var password = Guid.NewGuid().ToString().Split("-")[0];
            Credentials[username] = password;

            SaveCredentials();

            return password;
        }

        private void SaveCredentials()
        {
            var credentialList = Credentials.Select(kvp =>
                new Credential
                {
                    Username = kvp.Key,
                    Password = kvp.Value
                }
            );
            var json = JsonConvert.SerializeObject(credentialList, Formatting.Indented);

            File.WriteAllText(path, json);
        }
    }

    public class TestCredentialService : ICredentialService
    {
        public bool IsValidReturn = false;

        public string AddOrResetUser(string username)
        {
            return "";
        }

        public bool IsValid(string username, string password)
        {
            return IsValidReturn;
        }
    }
}
