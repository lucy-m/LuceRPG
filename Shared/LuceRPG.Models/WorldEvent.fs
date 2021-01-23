namespace LuceRPG.Models

module WorldEvent =
    type Model =
        | Moved of Id.WorldObject * Direction * byte

type WorldEvent = WorldEvent.Model
