namespace LuceRPG.Server.Core

open LuceRPG.Models

module ToLogString =

    let processResult
            (timestamp: int64)
            (result: IntentionProcessing.ProcessResult)
            : string seq =

        let typeStr = "Process Result"

        let events =
            result.events
            |> Seq.map WorldEventPayload.create
            |> Seq.map (fun p -> FormatPayload.format p "Events")

        let delayed =
            result.delayed
            |> Seq.map IndexedIntentionPayload.create
            |> Seq.map (fun p -> FormatPayload.format p "Delayed")

        let logs =
            Seq.append events delayed
            |> Seq.map (fun f ->  f typeStr timestamp)

        logs

    let clientJoined
            (timestamp: int64)
            (clientId: string)
            (username: string)
            : string =

        let payload = seq {
            sprintf "ClientId %s" clientId
            sprintf "Username %s" username
        }

        FormatPayload.format payload "Joined" "Client Joined" timestamp
