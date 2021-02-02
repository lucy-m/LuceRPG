namespace LuceRPG.Models

module ClientLogEntry =
    type Model =
        | ProcessResult of WorldEvent List * IndexedIntention List
        | UpdateIgnored of WorldEvent
        | ConsistencyCheckFailed of WorldDiff

    let createFromProcessResult (processResult: IntentionProcessing.ProcessResult): Model =
        Model.ProcessResult
            (processResult.events |> Seq.toList, processResult.delayed |> Seq.toList)

type LogEntry = ClientLogEntry.Model
