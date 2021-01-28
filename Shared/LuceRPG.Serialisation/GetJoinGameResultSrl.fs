namespace LuceRPG.Serialisation

open LuceRPG.Models

module GetJoinGameResultSrl =

    let serialise (result: GetJoinGameResult): byte[] =
        let label =
            match result with
            | GetJoinGameResult.Success _ -> 1uy
            | GetJoinGameResult.Failure _ -> 2uy
            | GetJoinGameResult.IncorrectCredentials -> 3uy

        let addtInfo =
            match result with
            | GetJoinGameResult.Success (cId, oId, w) ->
                Array.concat [
                    (StringSrl.serialise cId)
                    (StringSrl.serialise oId)
                    (WithTimestampSrl.serialise WorldSrl.serialise w)
                ]
            | GetJoinGameResult.Failure s -> StringSrl.serialise s
            | GetJoinGameResult.IncorrectCredentials -> [||]

        Array.append [|label|] addtInfo

    let deserialise (bytes: byte[]): GetJoinGameResult DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): GetJoinGameResult DesrlResult =
            match tag with
            | 1uy ->
                DesrlUtil.getThree
                    StringSrl.deserialise
                    StringSrl.deserialise
                    (WithTimestampSrl.deserialise WorldSrl.deserialise)
                    (fun cId oId w -> GetJoinGameResult.Success(cId, oId, w))
                    objectBytes
            | 2uy ->
                StringSrl.deserialise objectBytes
                |> DesrlResult.map (fun s -> GetJoinGameResult.Failure s)
            | 3uy -> DesrlResult.create GetJoinGameResult.IncorrectCredentials 0
            | _ ->
                printfn "Unknown GetJoinGameResult tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes
