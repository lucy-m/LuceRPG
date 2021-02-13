using LuceRPG.Adapters;
using LuceRPG.Models;
using LuceRPG.Serialisation;
using LuceRPG.Server.Processors;
using LuceRPG.Utility;
using LuceRPGServer.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FSharp.Collections;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LuceRPG.Server.Test
{
    public class WorldControllerTests
    {
        protected WorldModule.Model initialWorld;
        protected PointModule.Model spawnPoint;

        protected IntentionProcessor intentionProcessor;
        protected StaleClientProcessor staleClientProcessor;
        protected TestCredentialService credentialService;
        protected TestTimestampProvider timestampProvider;
        protected WorldController worldController;

        protected WorldEventsStorer worldStorer;
        protected IntentionQueue intentionQueue;
        protected LastPingStorer pingStorer;

        protected HttpRequest request;

        protected readonly string username = "username";

        protected static FileContentResult AsFileContentResult(ActionResult result)
        {
            Assert.That(result, Is.InstanceOf<FileContentResult>());
            return result as FileContentResult;
        }

        [SetUp]
        public void Setup()
        {
            timestampProvider = new TestTimestampProvider() { Now = 100L };

            var worldBounds = new RectModule.Model[]
            {
                new RectModule.Model(PointModule.create(0, 0), PointModule.create(10, 10))
            };
            spawnPoint = PointModule.create(5, 5);

            initialWorld = WorldModule.empty(worldBounds, spawnPoint);

            worldStorer = new WorldEventsStorer(initialWorld, InteractionStore.Empty(), timestampProvider);
            intentionQueue = new IntentionQueue(timestampProvider);
            pingStorer = new LastPingStorer();
            var logService = new TestCsvLogService();

            intentionProcessor = new IntentionProcessor(new NullLogger<IntentionProcessor>(), worldStorer, intentionQueue, logService, timestampProvider);
            staleClientProcessor = new StaleClientProcessor(new NullLogger<StaleClientProcessor>(), intentionQueue, pingStorer, timestampProvider);
            credentialService = new TestCredentialService();

            var httpContext = new DefaultHttpContext();
            request = httpContext.Request;
            var controllerContext = new ControllerContext() { HttpContext = httpContext };

            worldController = new WorldController(new NullLogger<WorldController>(), intentionQueue,
                worldStorer, pingStorer, credentialService, timestampProvider, logService)
            {
                ControllerContext = controllerContext
            };
        }

        [Test]
        public void JoinGameDisallowedCredentials()
        {
            credentialService.IsValidReturn = false;
            var result = worldController.JoinGame(username, "not password");

            var fileResult = AsFileContentResult(result);

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
                var fileResult = AsFileContentResult(result);
                var deserialised = GetJoinGameResultSrl.deserialise(fileResult.FileContents);

                Assert.That(deserialised.HasValue(), Is.True);
                Assert.That(deserialised.Value.value.IsFailure, Is.True);
            }

            [Test]
            public void AfterIntentionProcessing()
            {
                intentionProcessor.Process();

                // Adds a player object to the world
                var players =
                    worldStorer
                    .CurrentWorld
                    .objects
                    .Where(kvp => kvp.Value.value.t.IsPlayer)
                    .ToArray();

                Assert.That(players.Length, Is.EqualTo(1));

                // Player is at the spawn point
                var playerObj = players.Single().Value;
                Assert.That(playerObj.value.btmLeft, Is.EqualTo(spawnPoint));

                // Player has username passed in
                var playerData =
                    ((WorldObjectModule.TypeModule.Model.Player)playerObj.value.t)
                    .Item;
                Assert.That(playerData.name, Is.EqualTo(username));

                // Result is success result
                var result = joinGameTask.Result;
                var fileResult = AsFileContentResult(result);
                var deserialised = GetJoinGameResultSrl.deserialise(fileResult.FileContents);

                Assert.That(deserialised.HasValue(), Is.True);
                Assert.That(deserialised.Value.value.IsSuccess, Is.True);

                var success = ((GetJoinGameResultModule.Model.Success)deserialised.Value.value).Item;

                // Result contains newly added player id
                Assert.That(success.playerObjectId, Is.EqualTo(playerObj.id));

                // Result contains correct world
                var tsWorld = success.tsWorld;
                Assert.That(tsWorld.timestamp, Is.EqualTo(timestampProvider.Now));
                Assert.That(tsWorld.value, Is.EqualTo(worldStorer.CurrentWorld));

                // Object client map correct
                var ocMapEntry = MapModule.TryFind(playerObj.id, worldStorer.ServerSideData.objectClientMap);
                Assert.That(ocMapEntry.HasValue(), Is.True);
                Assert.That(ocMapEntry.Value, Is.EqualTo(success.clientId));
            }
        }

        protected class WorldWithPlayers : WorldControllerTests
        {
            protected string user1;
            protected string user2;
            protected string user3;

            protected string playerId1;
            protected string playerId2;
            protected string playerId3;

            protected string client1;
            protected string client2;
            protected string client3;

            [SetUp]
            public void SetUp_WorldWithPlayers()
            {
                user1 = "user1";
                user2 = "user2";
                user3 = "user3";

                credentialService.IsValidReturn = true;

                worldController.JoinGame(user1, "");
                worldController.JoinGame(user2, "");
                worldController.JoinGame(user3, "");

                intentionProcessor.Process();

                // world has three objects
                var worldObjects = WorldModule.objectList(worldStorer.CurrentWorld).ToArray();
                Assert.That(worldObjects.Length, Is.EqualTo(3));

                string GetPlayerId(string playerName)
                {
                    var playerObj =
                        worldObjects
                        .Single(o =>
                            o.value.t.IsPlayer
                            && ((WorldObjectModule.TypeModule.Model.Player)o.value.t)
                                    .Item.name == playerName
                        );

                    return playerObj.id;
                }

                string GetClientId(string playerId)
                {
                    var clientId = MapModule.Find(playerId, worldStorer.ServerSideData.objectClientMap);
                    return clientId;
                }

                playerId1 = GetPlayerId(user1);
                playerId2 = GetPlayerId(user2);
                playerId3 = GetPlayerId(user3);

                client1 = GetClientId(playerId1);
                client2 = GetClientId(playerId2);
                client3 = GetClientId(playerId3);

                Assert.That(playerId1, Is.Not.EqualTo(playerId2));
                Assert.That(playerId1, Is.Not.EqualTo(playerId3));
                Assert.That(playerId2, Is.Not.EqualTo(playerId3));

                Assert.That(client1, Is.Not.EqualTo(client2));
                Assert.That(client1, Is.Not.EqualTo(client3));
                Assert.That(client2, Is.Not.EqualTo(client3));
            }

            [Test]
            public void GetSinceBeforeClientsJoined()
            {
                var timestamp = timestampProvider.Now - 10L;

                var result = worldController.GetSince(timestamp, client1);
                var fileResult = AsFileContentResult(result);
                var deserialised = GetSinceResultSrl.deserialise(fileResult.FileContents);

                Assert.That(deserialised.HasValue(), Is.True);
                Assert.That(deserialised.Value.value.value.IsEvents, Is.True);
                Assert.That(deserialised.Value.value.timestamp, Is.EqualTo(timestampProvider.Now));

                var events =
                    ((GetSinceResultModule.Payload.Events)deserialised.Value.value.value)
                    .Item
                    .ToArray();

                var addedEvents =
                    events
                    .Where(e => e.value.t.IsObjectAdded)
                    .Select(e => (WorldEventModule.Type.ObjectAdded)e.value.t)
                    .ToArray();

                Assert.That(events.Length, Is.EqualTo(3));

                void AssertAddEventExists(string playerId)
                {
                    var matching = addedEvents.Where(e => e.Item.id == playerId);
                    Assert.That(matching.Count(), Is.EqualTo(1));
                }

                AssertAddEventExists(playerId1);
                AssertAddEventExists(playerId2);
                AssertAddEventExists(playerId3);
            }

            [Test]
            public void IntentionFromCorrectClient()
            {
                timestampProvider.Now = 120L;

                var direction = DirectionModule.Model.South;
                var t = IntentionModule.Type.NewMove(playerId1, direction, 1);
                var payload = IntentionModule.makePayload(client1, t);
                var intention = WithId.create(payload);
                var bytes = IntentionSrl.serialise(intention);

                request.Body = new MemoryStream(bytes);

                worldController.Intention().Wait();

                // Adds item to the queue
                Assert.That(intentionQueue.Queue.Count, Is.EqualTo(1));
                var intentionId = intentionQueue.Queue.First().Intention.tsIntention.value.id;

                intentionProcessor.Process();

                // Player is moved correctly
                var newWorld = worldStorer.CurrentWorld;
                var player1 = newWorld.objects[playerId1];
                var newPlayerPos = DirectionModule.movePoint(direction, 1, spawnPoint);

                Assert.That(player1.value.btmLeft, Is.EqualTo(newPlayerPos));
                Assert.That(newWorld.objects[playerId2].value.btmLeft, Is.EqualTo(spawnPoint));
                Assert.That(newWorld.objects[playerId3].value.btmLeft, Is.EqualTo(spawnPoint));

                // Player is marked as busy
                var expectedBusyEnd = timestampProvider.Now + WorldObjectModule.travelTime(player1.value);

                Assert.That(worldStorer.ObjectBusyMap[playerId1], Is.EqualTo(expectedBusyEnd));
                Assert.That(worldStorer.ObjectBusyMap.ContainsKey(playerId2), Is.False);
                Assert.That(worldStorer.ObjectBusyMap.ContainsKey(playerId3), Is.False);

                // Get since 110L returns only move event
                var result = worldController.GetSince(110L, client1);
                var fileContentResult = AsFileContentResult(result);
                var deserialised = GetSinceResultSrl.deserialise(fileContentResult.FileContents);
                var asEvents =
                    ((GetSinceResultModule.Payload.Events)deserialised.Value.value.value)
                    .Item
                    .ToArray();

                Assert.That(asEvents.Length, Is.EqualTo(1));
                Assert.That(asEvents[0].value.t.IsMoved, Is.True);
                Assert.That(asEvents[0].value.resultOf, Is.EqualTo(intentionId));

                var moveEvent = (WorldEventModule.Type.Moved)asEvents[0].value.t;

                Assert.That(moveEvent.Item1, Is.EqualTo(playerId1));
                Assert.That(moveEvent.Item2, Is.EqualTo(direction));

                // Dispatching the intention again
                request.Body = new MemoryStream(bytes);
                worldController.Intention().Wait();

                // Adds item to the queue
                Assert.That(intentionQueue.Queue.Count, Is.EqualTo(1));
                var intentionId2 = intentionQueue.Queue.First().Intention.tsIntention.value.id;

                intentionProcessor.Process();

                // Player is not moved
                Assert.That(newWorld.objects[playerId1].value.btmLeft, Is.EqualTo(newPlayerPos));

                // Intention is delayed
                Assert.That(intentionQueue.Queue.Count, Is.EqualTo(1));
                Assert.That(
                    intentionQueue.Queue.First().Intention.tsIntention.value.id,
                    Is.EqualTo(intentionId2)
                );

                // Processing after busy time
                var busyEnd = worldStorer.ObjectBusyMap[playerId1];
                timestampProvider.Now = busyEnd;
                intentionProcessor.Process();

                // Intention is not delayed
                Assert.That(intentionQueue.Queue.Count, Is.EqualTo(0));

                // Player is moved
                var newPlayerPos2 = DirectionModule.movePoint(direction, 1, newPlayerPos);
                var newWorld2 = worldStorer.CurrentWorld;
                Assert.That(newWorld2.objects[playerId1].value.btmLeft, Is.EqualTo(newPlayerPos2));
            }

            [Test]
            public void IntentionFromIncorrectClient()
            {
                var direction = DirectionModule.Model.South;
                var t = IntentionModule.Type.NewMove(playerId1, direction, 1);
                var payload = IntentionModule.makePayload(client2, t);
                var intention = WithId.create(payload);
                var bytes = IntentionSrl.serialise(intention);

                request.Body = new MemoryStream(bytes);

                worldController.Intention().Wait();

                // Adds item to the queue
                Assert.That(intentionQueue.Queue.Count, Is.EqualTo(1));

                intentionProcessor.Process();

                // Player is not moved
                var newWorld = worldStorer.CurrentWorld;

                Assert.That(newWorld.objects[playerId1].value.btmLeft, Is.EqualTo(spawnPoint));
                Assert.That(newWorld.objects[playerId2].value.btmLeft, Is.EqualTo(spawnPoint));
                Assert.That(newWorld.objects[playerId3].value.btmLeft, Is.EqualTo(spawnPoint));

                // Player is not marked as busy
                Assert.That(worldStorer.ObjectBusyMap.ContainsKey(playerId1), Is.False);
                Assert.That(worldStorer.ObjectBusyMap.ContainsKey(playerId2), Is.False);
                Assert.That(worldStorer.ObjectBusyMap.ContainsKey(playerId3), Is.False);

                // Get since 110L returns empty
                var result = worldController.GetSince(110L, client1);
                var fileContentResult = AsFileContentResult(result);
                var deserialised = GetSinceResultSrl.deserialise(fileContentResult.FileContents);
                var asEvents =
                    ((GetSinceResultModule.Payload.Events)deserialised.Value.value.value)
                    .Item
                    .ToArray();

                Assert.That(asEvents.Length, Is.EqualTo(0));
            }
        }
    }
}
