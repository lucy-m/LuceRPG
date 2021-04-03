namespace LuceRPG.Models

module Intention =
    type Type =
        | Move of Id.WorldObject * Direction * byte
        | TurnTowards of Id.WorldObject * Direction
        | JoinWorld of WorldObject
        | JoinGame of string
        | LeaveGame
        | LeaveWorld
        | Warp of Id.World * Point * Id.WorldObject

    type Payload =
        {
            clientId: Id.Client
            t: Type
        }

    let makePayload (clientId: Id.Client) (t: Type) =
        {
            clientId = clientId
            t = t
        }

    type Model = Payload WithId

type Intention = Intention.Model

module IndexedIntention =
    type Model =
        {
            tsIntention: Intention WithTimestamp
            worldId: Id.World
            index: int
        }

    let useIndex (index: int) (worldId: Id.World) (tsIntention: Intention WithTimestamp): Model =
        {
            tsIntention = tsIntention
            worldId = worldId
            index = index
        }

    let create = useIndex 0

    let applyToAllWorlds (model: Model): bool =
        match model.tsIntention.value.value.t with
        | Intention.Type.LeaveGame -> true
        | _ -> false

type IndexedIntention = IndexedIntention.Model

