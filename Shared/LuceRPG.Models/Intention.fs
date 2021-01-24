namespace LuceRPG.Models

module Intention =
    type Type =
        | Move of Id.WorldObject * Direction * byte
        | JoinGame

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
