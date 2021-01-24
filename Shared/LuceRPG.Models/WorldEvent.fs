namespace LuceRPG.Models

module WorldEvent =
    type Type =
        | Moved of Id.WorldObject * Direction * byte
        | GameJoined of Id.WorldObject

    type Model =
        {
            resultOf: Id.Intention
            t: Type
        }

    let asResult (intention: Id.Intention) (t: Type): Model =
        {
            resultOf = intention
            t = t
        }

type WorldEvent = WorldEvent.Model
