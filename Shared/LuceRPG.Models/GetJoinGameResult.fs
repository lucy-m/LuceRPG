namespace LuceRPG.Models

module GetJoinGameResult =
    module SuccessPayload =
        type Model =
            {
                clientId: string
                playerObjectId: string
                tsWorld: World WithTimestamp
            }

        let create
                (clientId: string)
                (playerObjectId: string)
                (tsWorld: World WithTimestamp)
                : Model =
            {
                clientId = clientId
                playerObjectId = playerObjectId
                tsWorld = tsWorld
            }

    type SuccessPayload = SuccessPayload.Model

    type Model =
        | Success of SuccessPayload
        | Failure of string
        | IncorrectCredentials

type GetJoinGameResult = GetJoinGameResult.Model
