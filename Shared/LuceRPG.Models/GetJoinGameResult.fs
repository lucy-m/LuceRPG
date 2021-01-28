namespace LuceRPG.Models

module GetJoinGameResult =
    type Model =
        | Success of Id.Client * Id.WorldObject * World WithTimestamp
        | Failure of string
        | IncorrectCredentials

type GetJoinGameResult = GetJoinGameResult.Model
