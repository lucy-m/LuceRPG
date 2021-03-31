namespace LuceRPG.Server.Core

open LuceRPG.Models

module ToLogString =

    let processResult
            (timestamp: int64)
            (result: IntentionProcessing.ProcessManyResult)
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

        let logs =
            result.logs
            |> Seq.map (fun l -> Seq.singleton l, "Log")

        Seq.concat [ events; delayed; logs ]
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

    let behaviourUpdateResult
            (timestamp: int64)
            (result: BehaviourMap.UpdateResult)
            : string seq =

        result.logs
        |> Seq.map (fun log ->
            let payload = Seq.singleton log
            let subType = "Behaviour"
            let eventType = "Behaviour"
            let timestamp = timestamp

            FormatFields.format payload subType eventType timestamp
        )
