using LuceRPG.Adapters;
using LuceRPG.Models;

public class WorldStore
{
    public string PlayerId { get; set; }
    public WorldModule.Model World { get; set; }
    public InteractionStore Interactions { get; set; }
    public long LastUpdate { get; set; }

    public void Apply(WorldEventModule.Model worldEvent)
    {
        World = EventApply.apply(worldEvent, World);
    }

    public bool HasWorld()
    {
        return World != null;
    }
}
