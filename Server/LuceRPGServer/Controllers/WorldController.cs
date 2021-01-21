using LuceRPG.Samples;
using LuceRPG.Serialisation;
using LuceRPG.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace LuceRPGServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorldController : ControllerBase
    {
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
            var serialised = WorldSrl.serialise(world);
            return File(serialised, "application/octet-stream");
        }

        [HttpPut("Intention")]
        public async Task Intention()
        {
            var buffer = new byte[20];
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
