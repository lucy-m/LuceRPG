namespace LuceRPG.Models

module GetJoinGameResult =
    type Payload =
        | Success of Id.WorldObject * World WithTimestamp
        | Failure of string

    type Model = Payload WithId

type GetJoinGameResult = GetJoinGameResult.Model
