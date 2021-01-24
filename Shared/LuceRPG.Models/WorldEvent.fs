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

    let getObjectId (t: Type): Id.WorldObject Option =
        match t with
        | Type.Moved (id, _, _) -> id |> Option.Some
        | Type.GameJoined id -> id |> Option.Some

type WorldEvent = WorldEvent.Model
