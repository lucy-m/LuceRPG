namespace LuceRPG.Models

module Interaction =
    type One =
        | Chat of string

    type Payload = One List
    type Model = Payload WithId

    type Store = Map<Id.Interaction, Model>

type Interaction = Interaction.Model
