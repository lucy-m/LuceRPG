using Microsoft.AspNetCore.Mvc;
using LuceRPG.Models;
using LuceRPG.Serialisation;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace LuceRPGServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorldController : ControllerBase
    {
        private static WorldModule.Model CreateSampleWorld()
        {
            var bound = RectModule.create(1, -1, 10, 9);
            var bounds = new FSharpList<RectModule.Model>(bound, FSharpList<RectModule.Model>.Empty);

            var emptyWorld = WorldModule.empty(bounds);

            var wall = WorldObjectModule.create(WorldObjectModule.TypeModule.Model.Wall, PointModule.create(8, 6));

            var world = WorldModule.addObject(wall, emptyWorld);

            return FSharpOption<WorldModule.Model>.get_IsSome(world) ? world.Value : emptyWorld;
        }

        [HttpGet]
        public ActionResult Get()
        {
            var w = CreateSampleWorld();

            var serialised = WorldSrl.serialise(w);

            return File(serialised, "application/octet-stream");
        }
    }
}
