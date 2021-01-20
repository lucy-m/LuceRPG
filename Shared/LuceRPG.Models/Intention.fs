namespace LuceRPG.Models

module Intention =
    type Model =
        | Move of Id.WorldObject * Direction * byte

type Intention = Intention.Model
