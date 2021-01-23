namespace LuceRPG.Models

module Intention =
    type Payload =
        | Move of Id.WorldObject * Direction * byte

    type Model = Payload WithGuid

type Intention = Intention.Model
