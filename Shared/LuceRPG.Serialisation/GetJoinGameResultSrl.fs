namespace LuceRPG.Serialisation

open LuceRPG.Models

module GetJoinGameResultSrl =

    let serialiseSuccessPayload (p: GetJoinGameResult.SuccessPayload): byte[] =
        Array.concat [
            StringSrl.serialise p.clientId
            StringSrl.serialise p.playerObjectId
            WithTimestampSrl.serialise WorldSrl.serialise p.tsWorld
            ListSrl.serialise InteractionSrl.serialise p.interactions
        ]

    let serialise (result: GetJoinGameResult): byte[] =
        let label =
            match result with
            | GetJoinGameResult.Success _ -> 1uy
            | GetJoinGameResult.Failure _ -> 2uy
            | GetJoinGameResult.IncorrectCredentials -> 3uy

        let addtInfo =
            match result with
            | GetJoinGameResult.Success p -> serialiseSuccessPayload p
            | GetJoinGameResult.Failure s -> StringSrl.serialise s
            | GetJoinGameResult.IncorrectCredentials -> [||]

        Array.append [|label|] addtInfo

    let deserialisePayload (bytes: byte[]): GetJoinGameResult.SuccessPayload DesrlResult =
        DesrlUtil.getFour
            StringSrl.deserialise
            StringSrl.deserialise
            (WithTimestampSrl.deserialise WorldSrl.deserialise)
            (ListSrl.deserialise InteractionSrl.deserialise)
            GetJoinGameResult.SuccessPayload.create
            bytes

    let deserialise (bytes: byte[]): GetJoinGameResult DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): GetJoinGameResult DesrlResult =
            match tag with
            | 1uy ->
                deserialisePayload objectBytes
                |> DesrlResult.map GetJoinGameResult.Success
            | 2uy ->
                StringSrl.deserialise objectBytes
                |> DesrlResult.map (fun s -> GetJoinGameResult.Failure s)
            | 3uy -> DesrlResult.create GetJoinGameResult.IncorrectCredentials 0
            | _ ->
                printfn "Unknown GetJoinGameResult tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes
