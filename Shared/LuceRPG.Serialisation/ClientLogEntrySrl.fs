namespace LuceRPG.Serialisation

open LuceRPG.Models

module ClientLogEntry =

    let serialise (logEntry: LogEntry): byte[] =
        let label =
            match logEntry with
            | LogEntry.ProcessResult _ -> 1uy
            | LogEntry.UpdateIgnored _ -> 2uy
            | LogEntry.ConsistencyCheckFailed _ -> 3uy

        let addtInfo =
            match logEntry with
            | LogEntry.ProcessResult (es, is) ->
                let worldEvents =
                    ListSrl.serialise WorldEventSrl.serialise es
                let intentions =
                    ListSrl.serialise IntentionSrl.serialiseIndexed is

                Array.append worldEvents intentions

            | LogEntry.UpdateIgnored e ->
                WorldEventSrl.serialise e
            | LogEntry.ConsistencyCheckFailed d ->
                WorldDiffSrl.serialise d

        Array.append [|label|] addtInfo

    let deserialise (bytes: byte[]): LogEntry DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): LogEntry DesrlResult =
            match tag with
            | 1uy ->
                DesrlUtil.getTwo
                    (ListSrl.deserialise WorldEventSrl.deserialise)
                    (ListSrl.deserialise IntentionSrl.deserialiseIndexed)
                    (fun es is -> ClientLogEntry.ProcessResult (es, is))
                    objectBytes
            | 2uy ->
                WorldEventSrl.deserialise objectBytes
                |> DesrlResult.map (fun e -> ClientLogEntry.UpdateIgnored e)
            | 3uy ->
                WorldDiffSrl.deserialise objectBytes
                |> DesrlResult.map (fun d -> ClientLogEntry.ConsistencyCheckFailed d)
            | _ ->
                printfn "Unknown LogEntry tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes
