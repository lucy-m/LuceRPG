namespace LuceRPG.Models

module WorldEvent =
    type Model =
        | Moved of System.Guid * Direction * byte

type WorldEvent = WorldEvent.Model
