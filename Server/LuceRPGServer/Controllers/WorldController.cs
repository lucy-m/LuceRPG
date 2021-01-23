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
using System.Threading.Tasks;

namespace LuceRPGServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorldController : ControllerBase
    {
        private const string RawBytesContentType = "application/octet-stream";

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
            var buffer = new byte[40];
            var read = await Request.Body.ReadAsync(buffer);

            if (read == buffer.Length)
            {
                throw new Exception("Intention received was larger than the buffer size");
            }

            var intention = IntentionSrl.deserialise(buffer);

            if (intention.HasValue())
            {
                _logger.LogDebug("Queueing intention");
                _queue.Enqueue(intention.Value.value);
            }
            else
            {
                _logger.LogWarning("Unable to deserialise intention");
            }
        }
    }
}
