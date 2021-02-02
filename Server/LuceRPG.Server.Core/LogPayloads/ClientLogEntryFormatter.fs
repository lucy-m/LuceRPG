namespace LuceRPG.Server.Core

open LuceRPG.Models

module ClientLogEntryFormatter =

    let create (cle: ClientLogEntry): string seq =
        match cle.value with
        | ClientLogEntry.ProcessResult (es, iis) ->
            let eventType = "Process Result"

            let events =
                es
                |> Seq.map WorldEventFields.create
                |> Seq.map (fun p -> p, "Event")

            let delayed =
                iis
                |> Seq.map IndexedIntentionFields.create
                |> Seq.map (fun p -> p, "Delayed")

            Seq.append events delayed
            |> Seq.map (fun (fields, subType) ->
                FormatFields.format fields subType eventType cle.timestamp
            )

        | ClientLogEntry.UpdateIgnored e ->
            let eventType = "Update Ignored"
            let fields = WorldEventFields.create e

            seq { FormatFields.format fields "" eventType cle.timestamp}

        | ClientLogEntry.ConsistencyCheckFailed wd ->
            let eventType = "Consistency Check Failed"

            wd
            |> List.map WorldDiffFields.create
            |> List.map (fun (subType, fields) ->
                FormatFields.format fields subType eventType cle.timestamp
            )
            |> List.toSeq
