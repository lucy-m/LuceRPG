namespace LuceRPG.Game.Streams
{
    public class StreamRegistry
    {
        public WorldEventStream WorldEvents { get; } = new WorldEventStream();
    }
}
