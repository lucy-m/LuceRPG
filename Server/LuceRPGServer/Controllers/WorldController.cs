using LuceRPG.Models;
using LuceRPG.Serialisation;
using LuceRPG.Server;
using LuceRPG.Server.Core;
using LuceRPG.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
                            .Select(e => (WorldEventModule.Type.ObjectAdded)e.t);
                    var playerAdded = objectAdded.FirstOrDefault(a => a.Item.value.t.IsPlayer);

                    if (playerAdded != null)
                    {
                        playerObject = playerAdded.Item;
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

                _logger.LogDebug($"Join game result player ID {playerObject?.id}");

                var joinGameResult = playerObject != null
                    ? GetJoinGameResultModule.Model.NewSuccess(
                        clientId,
                        playerObject.id,
                        WithTimestamp.create(_timestampProvider.Now, _worldStore.CurrentWorld)
                    )
                    : GetJoinGameResultModule.Model.NewFailure("Could not join game");

                var serialised = GetJoinGameResultSrl.serialise(joinGameResult);

                return File(serialised, RawBytesContentType);
            }
        }

        [HttpGet("since")]
        public ActionResult GetSince(long timestamp, string clientId)
        {
            var result = _worldStore.GetSince(timestamp);
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
            var result = _worldStore.CurrentWorld;
            var serialised = WorldSrl.serialise(result);

            return File(serialised, RawBytesContentType);
        }

        [HttpGet("dump")]
        public string Dump()
        {
            var world = _worldStore.CurrentWorld;
            var dump = ASCIIWorld.dump(world);

            Console.WriteLine(dump);

            return dump;
        }

        [HttpPut("intention")]
        public async Task Intention()
        {
            var buffer = new byte[200];
            var read = await Request.Body.ReadAsync(buffer);

            if (read == buffer.Length)
            {
                throw new Exception("Intention received was larger than the buffer size");
            }

            var intention = IntentionSrl.deserialise(buffer);

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
            var buffer = new byte[1000];
            var read = await Request.Body.ReadAsync(buffer);

            if (read == buffer.Length)
            {
                throw new Exception("Logs received were larger than the buffer size");
            }

            var logs = ClientLogEntrySrl.deserialiseLog(buffer);

            if (logs.HasValue())
            {
                _logService.AddClientLogs(clientId, logs.Value.value);
            }
        }
    }
}
