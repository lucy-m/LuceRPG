using Microsoft.AspNetCore.Mvc;
using LuceRPG.Models;
using LuceRPG.Serialisation;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using LuceRPG.Samples;

namespace LuceRPGServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorldController : ControllerBase
    {
        [HttpGet]
        public ActionResult Get()
        {
            var world = SampleWorlds.world1;
            var serialised = WorldSrl.serialise(world);
            return File(serialised, "application/octet-stream");
        }
    }
}
