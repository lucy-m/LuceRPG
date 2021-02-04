namespace LuceRPG.Models

module Interaction =
    type One =
        | Chat of string

    type Payload = One List
    type Model = Payload WithId

    type Store = Map<Id.Interaction, Model>

    let storeToList (store: Store): Model List =
        store
        |> Map.toList
        |> List.map snd

type Interaction = Interaction.Model
