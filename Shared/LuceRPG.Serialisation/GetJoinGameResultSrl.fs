namespace LuceRPG.Serialisation

open LuceRPG.Models

module GetJoinGameResultSrl =
    let serialise (result: GetJoinGameResult): byte[] =
        let label =
            match result with
            | GetJoinGameResult.Success _ -> 1uy
            | GetJoinGameResult.Failure _ -> 2uy

        let addtInfo =
            match result with
            | GetJoinGameResult.Success w -> WorldSrl.serialise w
            | GetJoinGameResult.Failure s -> StringSrl.serialise s

        Array.append [|label|] addtInfo

    let deserialise (bytes: byte[]): GetJoinGameResult DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): GetJoinGameResult DesrlResult =
            match tag with
            | 1uy ->
                WorldSrl.deserialise objectBytes
                |> DesrlResult.map (fun w -> GetJoinGameResult.Success w)
            | 2uy ->
                StringSrl.deserialise objectBytes
                |> DesrlResult.map (fun s -> GetJoinGameResult.Failure s)
            | _ ->
                printfn "Unknown GetJoinGameResult tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes
