namespace LuceRPG.Models

module Intention =
    type Payload =
        | Move of Id.WorldObject * Direction * byte
        | JoinGame

    type Model = Payload WithId

type Intention = Intention.Model
