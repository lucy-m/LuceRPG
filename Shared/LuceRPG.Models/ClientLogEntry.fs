namespace LuceRPG.Models

module ClientLogEntry =
    type Payload =
        | ProcessResult of WorldEvent List * IndexedIntention List
        | UpdateIgnored of WorldEvent
        | ConsistencyCheckFailed of WorldDiff

    type Model = Payload WithTimestamp

    let createFromProcessResult (processResult: IntentionProcessing.ProcessResult): Payload =
        ProcessResult
            (processResult.events |> Seq.toList, processResult.delayed |> Seq.toList)

type ClientLogEntry = ClientLogEntry.Model
type ClientLog = ClientLogEntry List
