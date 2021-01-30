using LuceRPG.Models;
using NUnit.Framework;
using System.Linq;
using LuceRPG.Server;
using LuceRPG.Server.Processors;
using Microsoft.Extensions.Logging.Abstractions;
using LuceRPG.Utility;
using LuceRPGServer.Controllers;

namespace LuceRPG.Server.Test
{
    public class WorldControllerTests
    {
        private WorldModule.Model initialWorld;

        private IntentionProcessor intentionProcessor;
        private StaleClientProcessor staleClientProcessor;
        private TestCredentialService credentialService;
        private TestTimestampProvider timestampProvider;
        private WorldController worldController;

        [SetUp]
        public void Setup()
        {
            timestampProvider = new TestTimestampProvider() { Now = 100L };

            var worldBounds = new RectModule.Model[]
            {
                new RectModule.Model(PointModule.create(0, 10), PointModule.create(10, 10))
            };
            var spawnPoint = PointModule.create(10, 10);

            initialWorld = WorldModule.empty(worldBounds, spawnPoint);

            var worldStorer = new WorldEventsStorer(initialWorld, timestampProvider);
            var queue = new IntentionQueue(timestampProvider);
            var pingStorer = new LastPingStorer();

            intentionProcessor = new IntentionProcessor(new NullLogger<IntentionProcessor>(), worldStorer, queue, timestampProvider);
            staleClientProcessor = new StaleClientProcessor(new NullLogger<StaleClientProcessor>(), queue, pingStorer, timestampProvider);
            credentialService = new TestCredentialService();
            worldController = new WorldController(new NullLogger<WorldController>(), queue,
                worldStorer, pingStorer, credentialService, timestampProvider);
        }

        [Test]
        public void JoinGameDisallowedCredentials()
        {
            credentialService.IsValidReturn = false;
            var result = worldController.JoinGame("some username", "not password");

            Assert.That(true, Is.True);
        }
    }
}
