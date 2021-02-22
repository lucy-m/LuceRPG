namespace LuceRPG.Models

module GetSinceResult =
    type Payload =
        | Events of WorldEvent WithTimestamp List
        | World of World
        | WorldChanged of World * Interactions
        | Failure of string

    type Model = Payload WithTimestamp

type GetSinceResult = GetSinceResult.Model
