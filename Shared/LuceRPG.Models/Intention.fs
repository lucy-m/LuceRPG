namespace LuceRPG.Models

module Intention =
    type Payload =
        | Move of System.Guid * Direction * byte

    type Model = Payload WithGuid

type Intention = Intention.Model
