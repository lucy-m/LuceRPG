using Microsoft.AspNetCore.Mvc;
using LuceRPG.Serialisation;
using LuceRPG.Samples;
using Microsoft.Extensions.Logging;
using LuceRPG.Server;
using System.Threading.Tasks;
using System;

namespace LuceRPGServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorldController : ControllerBase
    {
        private readonly ILogger<WorldController> _logger;
        private readonly IntentionQueue _queue;

        public WorldController(ILogger<WorldController> logger, IntentionQueue queue)
        {
            _logger = logger;
            _queue = queue;
        }

        [HttpGet]
        public ActionResult Get()
        {
            var world = SampleWorlds.world1;
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
