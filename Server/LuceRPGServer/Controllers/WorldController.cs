using LuceRPG.Models;
using LuceRPG.Samples;
using LuceRPG.Serialisation;
using LuceRPG.Server;
using LuceRPG.Server.Core;
using LuceRPG.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
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
        private readonly WorldEventsStorer _store;

        public WorldController(
            ILogger<WorldController> logger,
            IntentionQueue queue,
            WorldEventsStorer store
        )
        {
            _logger = logger;
            _queue = queue;
            _store = store;
        }

        [HttpGet]
        public ActionResult Get()
        {
            var world = _store.CurrentWorld;
            var timestamp = TimestampProvider.Now;

            var timestampedWorld = new WithTimestamp.Model<WorldModule.Model>(timestamp, world);

            var serialised = WithTimestampSrl.serialise(
                new Func<WorldModule.Model, byte[]>(WorldSrl.serialise).ToFSharpFunc(),
                timestampedWorld
            );
            return File(serialised, RawBytesContentType);
        }

        [HttpGet("join")]
        public ActionResult JoinGame()
        {
            WithId.Model<WorldObjectModule.Payload>? playerObject = null;
            bool playerSet = false;

            var intention = WithId.create(IntentionModule.Payload.JoinGame);

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

                playerSet = true;
            }

            _queue.Enqueue(intention, Action);

            var attempts = 0;
            while (!playerSet && attempts < MaxJoinGameAttempts)
            {
                attempts++;
                Thread.Sleep(50);
            }

            _logger.LogDebug($"Join game result player ID {playerObject?.id}");

            return Ok();
        }

        [HttpGet("since")]
        public ActionResult GetSince(long timestamp)
        {
            var result = _store.GetSince(timestamp);
            var newTimestamp = TimestampProvider.Now;

            var timestampedResult = new WithTimestamp.Model<GetSinceResultModule.Payload>(newTimestamp, result);

            var serialised = GetSinceResultSrl.serialise(timestampedResult);

            return File(serialised, RawBytesContentType);
        }

        [HttpGet("dump")]
        public string Dump()
        {
            var world = _store.CurrentWorld;
            var dump = ASCIIWorld.dump(world);

            Console.WriteLine(dump);

            return dump;
        }

        [HttpPut("Intention")]
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
    }
}
