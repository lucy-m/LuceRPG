namespace LuceRPG.Models

module Intention =
    type Model =
        | Move of Direction * byte

type Intention = Intention.Model
