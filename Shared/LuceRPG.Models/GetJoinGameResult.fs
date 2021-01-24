namespace LuceRPG.Models

module GetJoinGameResult =
    type Model =
        | Success of World
        | Failure of string

type GetJoinGameResult = GetJoinGameResult.Model
