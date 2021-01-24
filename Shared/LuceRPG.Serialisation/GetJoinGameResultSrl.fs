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
            | GetJoinGameResult.Success (id, w) ->
                Array.append
                    (StringSrl.serialise id)
                    (WithTimestampSrl.serialise WorldSrl.serialise w)
            | GetJoinGameResult.Failure s -> StringSrl.serialise s

        Array.append [|label|] addtInfo

    let deserialise (bytes: byte[]): GetJoinGameResult DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): GetJoinGameResult DesrlResult =
            match tag with
            | 1uy ->
                DesrlUtil.getTwo
                    StringSrl.deserialise
                    (WithTimestampSrl.deserialise WorldSrl.deserialise)
                    (fun id w -> GetJoinGameResult.Success(id, w))
                    objectBytes
            | 2uy ->
                StringSrl.deserialise objectBytes
                |> DesrlResult.map (fun s -> GetJoinGameResult.Failure s)
            | _ ->
                printfn "Unknown GetJoinGameResult tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes
