namespace LuceRPG.Serialisation

open LuceRPG.Models

module GetSinceResultSrl =
    let serialiseEvents (es: WorldEvent WithTimestamp List): byte[] =
        let serialiseOne = WithTimestampSrl.serialise WorldEventSrl.serialise
        ListSrl.serialise serialiseOne es

    let deserialiseEvents (bytes: byte[]): WorldEvent WithTimestamp List DesrlResult =
        let deserialiseOne = WithTimestampSrl.deserialise WorldEventSrl.deserialise
        ListSrl.deserialise deserialiseOne bytes

    let serialisePayload (result: GetSinceResult.Payload): byte[] =
        let label =
            match result with
            | GetSinceResult.Events _ -> 1uy
            | GetSinceResult.World _ -> 2uy
            | GetSinceResult.Failure _ -> 3uy
            | GetSinceResult.WorldChanged _ -> 4uy

        let addtInfo =
            match result with
            | GetSinceResult.Events e -> serialiseEvents e
            | GetSinceResult.World w -> WorldSrl.serialise w
            | GetSinceResult.Failure f -> StringSrl.serialise f
            | GetSinceResult.WorldChanged (w,is) ->
                Array.append
                    (WorldSrl.serialise w)
                    (ListSrl.serialise InteractionSrl.serialise is)

        Array.append [|label|] addtInfo

    let deserialisePayload (bytes: byte[]): GetSinceResult.Payload DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): GetSinceResult.Payload DesrlResult =
            match tag with
            | 1uy ->
                deserialiseEvents objectBytes
                |> DesrlResult.map GetSinceResult.Events
            | 2uy ->
                WorldSrl.deserialise objectBytes
                |> DesrlResult.map GetSinceResult.World
            | 3uy ->
                StringSrl.deserialise objectBytes
                |> DesrlResult.map GetSinceResult.Failure
            | 4uy ->
                DesrlUtil.getTwo
                    WorldSrl.deserialise
                    (ListSrl.deserialise InteractionSrl.deserialise)
                    (fun w is -> GetSinceResult.WorldChanged(w, is))
                    objectBytes
            | _ ->
                printfn "Unknown GetResult tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes

    let serialise (result: GetSinceResult): byte[] =
        WithTimestampSrl.serialise serialisePayload result

    let deserialise (bytes: byte[]): GetSinceResult DesrlResult =
        WithTimestampSrl.deserialise deserialisePayload bytes
