namespace LuceRPG.Models

module WorldEvent =
    type Model =
        | Moved of Id.WorldObject * Direction * byte
        | GameJoined of Id.WorldObject

type WorldEvent = WorldEvent.Model
