using LuceRPG.Models;
using NUnit.Framework;
using System.Linq;
using LuceRPG.Server;
using LuceRPG.Server.Processors;
using Microsoft.Extensions.Logging.Abstractions;
using LuceRPG.Utility;
using LuceRPGServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using LuceRPG.Serialisation;
using System.Threading;
using System.Threading.Tasks;

namespace LuceRPG.Server.Test
{
    public class WorldControllerTests
    {
        protected WorldModule.Model initialWorld;

        protected IntentionProcessor intentionProcessor;
        protected StaleClientProcessor staleClientProcessor;
        protected TestCredentialService credentialService;
        protected TestTimestampProvider timestampProvider;
        protected WorldController worldController;

        protected WorldEventsStorer worldStorer;
        protected IntentionQueue intentionQueue;
        protected LastPingStorer pingStorer;

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

            worldStorer = new WorldEventsStorer(initialWorld, timestampProvider);
            intentionQueue = new IntentionQueue(timestampProvider);
            pingStorer = new LastPingStorer();

            intentionProcessor = new IntentionProcessor(new NullLogger<IntentionProcessor>(), worldStorer, intentionQueue, timestampProvider);
            staleClientProcessor = new StaleClientProcessor(new NullLogger<StaleClientProcessor>(), intentionQueue, pingStorer, timestampProvider);
            credentialService = new TestCredentialService();
            worldController = new WorldController(new NullLogger<WorldController>(), intentionQueue,
                worldStorer, pingStorer, credentialService, timestampProvider);
        }

        [Test]
        public void JoinGameDisallowedCredentials()
        {
            credentialService.IsValidReturn = false;
            var result = worldController.JoinGame("some username", "not password");

            Assert.That(result, Is.InstanceOf<FileContentResult>());

            var fileResult = result as FileContentResult;
            var deserialised = GetJoinGameResultSrl.deserialise(fileResult.FileContents);

            Assert.That(deserialised.HasValue(), Is.True);
            Assert.That(deserialised.Value.value.IsIncorrectCredentials, Is.True);
        }

        protected class JoinGame : WorldControllerTests
        {
            protected Task<ActionResult> joinGameTask;

            [SetUp]
            public void SetUp_JoinGame()
            {
                var username = "username";
                credentialService.IsValidReturn = true;

                joinGameTask =
                    Task<ActionResult>
                        .Factory
                        .StartNew(() => worldController.JoinGame(username, ""));
                Thread.Sleep(50);
            }

            [Test]
            public void DoesNotReturnImmediately()
            {
                Assert.That(joinGameTask.IsCompleted, Is.False);
            }

            [Test]
            public void AddsItemToIntentionQueue()
            {
                Assert.That(intentionQueue.Queue.Count, Is.EqualTo(1));
            }

            [Test]
            public void EventuallyReturnsGenericFailure()
            {
                var result = joinGameTask.Result;
                Assert.That(result, Is.InstanceOf<FileContentResult>());

                var fileResult = result as FileContentResult;
                var deserialised = GetJoinGameResultSrl.deserialise(fileResult.FileContents);

                Assert.That(deserialised.HasValue(), Is.True);
                Assert.That(deserialised.Value.value.IsFailure, Is.True);
            }
        }
    }
}
