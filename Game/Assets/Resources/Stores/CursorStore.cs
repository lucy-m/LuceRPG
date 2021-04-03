using LuceRPG.Models;

namespace LuceRPG.Game.Stores
{
    public class CursorStore
    {
        public PointModule.Model Position { get; set; }
        public WithId.Model<WorldObjectModule.Payload> CursorOverObject { get; set; }
    }
}
