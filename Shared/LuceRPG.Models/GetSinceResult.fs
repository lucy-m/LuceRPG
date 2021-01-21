namespace LuceRPG.Models

module GetSinceResult =
    type Model =
        | Events of WorldEvent WithTimestamp List
        | World of World

type GetSinceResult = GetSinceResult.Model
