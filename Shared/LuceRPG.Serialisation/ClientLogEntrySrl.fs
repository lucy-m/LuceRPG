namespace LuceRPG.Serialisation

open LuceRPG.Models

module ClientLogEntrySrl =

    let serialisePayload (logEntry: ClientLogEntry.Payload): byte[] =
        let label =
            match logEntry with
            | ClientLogEntry.ProcessResult _ -> 1uy
            | ClientLogEntry.UpdateIgnored _ -> 2uy
            | ClientLogEntry.ConsistencyCheckFailed _ -> 3uy

        let addtInfo =
            match logEntry with
            | ClientLogEntry.ProcessResult (es, is) ->
                let worldEvents =
                    ListSrl.serialise WorldEventSrl.serialise es
                let intentions =
                    ListSrl.serialise IntentionSrl.serialiseIndexed is

                Array.append worldEvents intentions

            | ClientLogEntry.UpdateIgnored e ->
                WorldEventSrl.serialise e
            | ClientLogEntry.ConsistencyCheckFailed d ->
                WorldDiffSrl.serialise d

        Array.append [|label|] addtInfo

    let serialise (logEntry: ClientLogEntry): byte[] =
        WithTimestampSrl.serialise serialisePayload logEntry

    let serialiseLog (log: ClientLog): byte[] =
        ListSrl.serialise serialise log

    let deserialisePayload (bytes: byte[]): ClientLogEntry.Payload DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): ClientLogEntry.Payload DesrlResult =
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

    let deserialise (bytes: byte[]): ClientLogEntry DesrlResult =
        WithTimestampSrl.deserialise deserialisePayload bytes

    let deserialiseLog (bytes: byte[]): ClientLog DesrlResult =
        ListSrl.deserialise deserialise bytes
