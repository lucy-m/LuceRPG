namespace LuceRPG.Server.Core

open LuceRPG.Models

module ToLogString =

    let processResult
            (timestamp: int64)
            (result: IntentionProcessing.ProcessResult)
            : string seq =

        let eventType = "Process Result"

        let events =
            result.events
            |> Seq.map WorldEventFields.create
            |> Seq.map (fun p -> p, "Event")

        let delayed =
            result.delayed
            |> Seq.map IndexedIntentionFields.create
            |> Seq.map (fun p -> p, "Delayed")

        Seq.append events delayed
        |> Seq.map (fun (fields, subType) ->
            FormatFields.format fields subType eventType timestamp
        )

    let clientJoined
            (timestamp: int64)
            (clientId: string)
            (username: string)
            : string =
        let payload = seq {
            sprintf "ClientId %s" clientId
            sprintf "Username %s" username
        }

        FormatFields.format payload "Joined" "Client Joined" timestamp

    let clientLog (entry: ClientLogEntry): string seq =
        ClientLogEntryFormatter.create entry
