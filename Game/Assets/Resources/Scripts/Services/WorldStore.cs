using LuceRPG.Models;

public class WorldStore
{
    public WorldModule.Model World { get; private set; }

    public void Apply(WorldEventModule.Model worldEvent)
    {
        World = EventApply.apply(worldEvent, World);
    }

    public bool HasWorld()
    {
        return World != null;
    }
}
