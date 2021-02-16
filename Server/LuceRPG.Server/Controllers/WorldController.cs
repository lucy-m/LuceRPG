using LuceRPG.Models;
using LuceRPG.Serialisation;
using LuceRPG.Server;
using LuceRPG.Server.Core;
using LuceRPG.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LuceRPGServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorldController : ControllerBase
    {
        private const string RawBytesContentType = "application/octet-stream";
        private const int MaxJoinGameAttempts = 10;

        private readonly ILogger<WorldController> _logger;
        private readonly IntentionQueue _queue;
        private readonly WorldEventsStorer _worldStore;
        private readonly LastPingStorer _pingStorer;
        private readonly ICredentialService _credentialService;
        private readonly ITimestampProvider _timestampProvider;
        private readonly ICsvLogService _logService;

        public WorldController(
            ILogger<WorldController> logger,
            IntentionQueue queue,
            WorldEventsStorer store,
            LastPingStorer pingStorer,
            ICredentialService credentialService,
            ITimestampProvider timestampProvider,
            ICsvLogService logService
        )
        {
            _logger = logger;
            _queue = queue;
            _worldStore = store;
            _pingStorer = pingStorer;
            _credentialService = credentialService;
            _timestampProvider = timestampProvider;
            _logService = logService;
        }

        [HttpGet("join")]
        public ActionResult JoinGame(string username, string password)
        {
            if (!_credentialService.IsValid(username, password))
            {
                _logger.LogWarning($"Invalid credentials submitted {username} {password}");
                var result = GetJoinGameResultModule.Model.IncorrectCredentials;
                var serialised = GetJoinGameResultSrl.serialise(result);
                return File(serialised, RawBytesContentType);
            }
            else
            {
                _logger.LogInformation($"Request to join game from {username}");

                WithId.Model<WorldObjectModule.Payload>? playerObject = null;
                string? worldId = null;

                bool intentionProcessed = false;
                var clientId = Guid.NewGuid().ToString();

                _logService.EstablishLog(clientId, username);

                var intention = WithId.create(
                    IntentionModule.makePayload(clientId, IntentionModule.Type.NewJoinGame(username))
                );

                void Action(IEnumerable<WorldEventModule.Model> events)
                {
                    var objectAdded =
                        events
                            .Where(e => e.t.IsObjectAdded)
                            .Select(e => (Event: e, ObjectAdded: ((WorldEventModule.Type.ObjectAdded)e.t).Item));
                    var playerAdded = objectAdded.Where(a => a.ObjectAdded.value.t.IsPlayer).ToArray();

                    if (playerAdded.Any())
                    {
                        var (Event, ObjectAdded) = playerAdded[0];

                        playerObject = ObjectAdded;
                        worldId = Event.world;
                    }

                    intentionProcessed = true;
                }

                _queue.Enqueue(intention, Action);

                var attempts = 0;
                while (!intentionProcessed && attempts < MaxJoinGameAttempts)
                {
                    attempts++;
                    Thread.Sleep(50);
                }

                var world = worldId != null ? _worldStore.GetWorld(worldId) : null;
                _logger.LogDebug($"Join game result player ID {playerObject?.id}");

                if (playerObject != null && world == null)
                {
                    _logger.LogError($"Player ID {playerObject.id} was added to an invalid world");
                }

                var joinGameResult = playerObject != null && world != null
                    ? GetJoinGameResultModule.Model.NewSuccess(
                        GetJoinGameResultModule.SuccessPayloadModule.create(
                        clientId,
                        playerObject.id,
                        WithTimestamp.create(_timestampProvider.Now, world),
                        WithId.toList(_worldStore.Interactions.Value)
                    ))
                    : GetJoinGameResultModule.Model.NewFailure("Could not join game");

                var serialised = GetJoinGameResultSrl.serialise(joinGameResult);

                return File(serialised, RawBytesContentType);
            }
        }

        [HttpGet("since")]
        public ActionResult GetSince(long timestamp, string clientId)
        {
            var result = _worldStore.GetSince(timestamp, clientId);
            var newTimestamp = _timestampProvider.Now;

            _pingStorer.Update(clientId, newTimestamp);
            var timestampedResult = new WithTimestamp.Model<GetSinceResultModule.Payload>(newTimestamp, result);

            var serialised = GetSinceResultSrl.serialise(timestampedResult);

            return File(serialised, RawBytesContentType);
        }

        [HttpGet("allState")]
        public ActionResult GetAllState(string clientId)
        {
            _logger.LogDebug($"Consistency check from {clientId}");

            var worldId = _worldStore.GetWorldIdForClient(clientId);
            if (worldId != null)
            {
                var result = _worldStore.GetWorld(worldId);

                if (result != null)
                {
                    var serialised = WorldSrl.serialise(result);

                    return File(serialised, RawBytesContentType);
                }
            }

            return Ok();
        }

        [HttpGet("dump")]
        public string Dump(string worldId)
        {
            var world = _worldStore.GetWorld(worldId);
            if (world != null)
            {
                var dump = ASCIIWorld.dump(world.value);

                Console.WriteLine(dump);

                return dump;
            }
            else
            {
                return "Unknown world ID";
            }
        }

        [HttpPut("intention")]
        public async Task Intention()
        {
            using var stream = Request.BodyReader.AsStream();
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            var intention = IntentionSrl.deserialise(bytes);

            if (intention.HasValue())
            {
                _queue.Enqueue(intention.Value.value);
            }
            else
            {
                _logger.LogWarning("Unable to deserialise intention");
            }
        }

        [HttpPut("logs")]
        public async Task PutLogs(string clientId)
        {
            using var stream = Request.BodyReader.AsStream();
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            var bytes = memoryStream.ToArray();

            _logger.LogInformation($"Got {bytes.Length} bytes as logs from {clientId}");

            var logs = ClientLogEntrySrl.deserialiseLog(bytes.ToArray());

            if (logs.HasValue())
            {
                _logService.AddClientLogs(clientId, logs.Value.value);
            }
        }
    }
}
