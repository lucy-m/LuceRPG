namespace LuceRPG.Models

module WorldEvent =
    type Type =
        | Moved of Id.WorldObject * Direction
        | ObjectAdded of WorldObject
        | ObjectRemoved of Id.WorldObject
        | JoinedWorld of Id.Client

    type Model =
        {
            resultOf: Id.Intention
            world: Id.World
            index: int
            t: Type
        }

    let asResult
            (intention: Id.Intention)
            (world: Id.World)
            (index: int)
            (t: Type)
            : Model =
        {
            resultOf = intention
            world = world
            index = index
            t = t
        }

    let getObjectId (t: Type): Id.WorldObject Option =
        match t with
        | Type.Moved (id, _) -> id |> Option.Some
        | Type.ObjectAdded o -> o.id |> Option.Some
        | Type.ObjectRemoved id -> id |> Option.Some
        | Type.JoinedWorld _ -> Option.None

type WorldEvent = WorldEvent.Model
