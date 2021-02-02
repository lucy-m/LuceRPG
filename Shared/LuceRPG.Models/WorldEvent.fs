namespace LuceRPG.Models

module WorldEvent =
    type Type =
        | Moved of Id.WorldObject * Direction
        | ObjectAdded of WorldObject
        | ObjectRemoved of Id.WorldObject

    type Model =
        {
            resultOf: Id.Intention
            index: int
            t: Type
        }

    let asResult (intention: Id.Intention) (index: int) (t: Type): Model =
        {
            resultOf = intention
            index = index
            t = t
        }

    let getObjectId (t: Type): Id.WorldObject Option =
        match t with
        | Type.Moved (id, _) -> id |> Option.Some
        | Type.ObjectAdded o -> o.id |> Option.Some
        | Type.ObjectRemoved id -> id |> Option.Some

type WorldEvent = WorldEvent.Model
