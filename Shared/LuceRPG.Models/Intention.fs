namespace LuceRPG.Models

module Intention =
    type Type =
        | Move of Id.WorldObject * Direction * byte
        | JoinGame of string
        | LeaveGame

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
            index: int
        }

    let create (tsIntention: Intention WithTimestamp): Model =
        {
            tsIntention = tsIntention
            index = 0
        }

    let useIndex (index: int) (tsIntention: Intention WithTimestamp): Model =
        {
            tsIntention = tsIntention
            index = index
        }

type IndexedIntention = IndexedIntention.Model

