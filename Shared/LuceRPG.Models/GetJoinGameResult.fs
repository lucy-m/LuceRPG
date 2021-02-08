namespace LuceRPG.Models

module GetJoinGameResult =
    module SuccessPayload =
        type Model =
            {
                clientId: string
                playerObjectId: string
                tsWorld: World WithTimestamp
                interactions: Interaction List
            }

        let create
                (clientId: string)
                (playerObjectId: string)
                (tsWorld: World WithTimestamp)
                (interactions: Interaction List)
                : Model =
            {
                clientId = clientId
                playerObjectId = playerObjectId
                tsWorld = tsWorld
                interactions = interactions
            }

    type SuccessPayload = SuccessPayload.Model

    type Model =
        | Success of SuccessPayload
        | Failure of string
        | IncorrectCredentials

type GetJoinGameResult = GetJoinGameResult.Model
