namespace LuceRPG.Models

module GetSinceResult =
    type Payload =
        | Events of WorldEvent WithTimestamp List
        | World of World

    type Model = Payload WithId WithTimestamp

type GetSinceResult = GetSinceResult.Model
