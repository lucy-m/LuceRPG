namespace LuceRPG.Serialisation

open LuceRPG.Models

module GetSinceResultSrl =
    let serialiseEvents (es: WorldEvent WithTimestamp List): byte[] =
        let serialiseOne = WithTimestampSrl.serialise WorldEventSrl.serialise
        ListSrl.serialise serialiseOne es

    let deserialiseEvents (bytes: byte[]): WorldEvent WithTimestamp List DesrlResult =
        let deserialiseOne = WithTimestampSrl.deserialise WorldEventSrl.deserialise
        ListSrl.deserialise deserialiseOne bytes

    let serialise (result: GetSinceResult): byte[] =
        let label =
            match result with
            | GetSinceResult.Events _ -> 1uy
            | GetSinceResult.World _ -> 2uy

        let addtInfo =
            match result with
            | GetSinceResult.Events e -> serialiseEvents e
            | GetSinceResult.World w -> WorldSrl.serialise w

        Array.append [|label|] addtInfo

    let deserialise (bytes: byte[]): GetSinceResult DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): GetSinceResult DesrlResult =
            match tag with
            | 1uy ->
                deserialiseEvents objectBytes
                |> DesrlResult.map (fun e -> GetSinceResult.Events e)
            | 2uy ->
                WorldSrl.deserialise objectBytes
                |> DesrlResult.map (fun w -> GetSinceResult.World w)
            | _ ->
                printfn "Unknown GetResult tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes
