namespace LuceRPG.Models

module WorldEvent =
    type Type =
        | Moved of Id.WorldObject * Direction * byte
        | ObjectAdded of WorldObject
        | ObjectRemoved of Id.WorldObject

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
        | Type.ObjectAdded o -> o.id |> Option.Some
        | Type.ObjectRemoved id -> id |> Option.Some

type WorldEvent = WorldEvent.Model
