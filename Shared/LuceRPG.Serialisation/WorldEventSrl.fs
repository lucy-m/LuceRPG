namespace LuceRPG.Serialisation

open LuceRPG.Models

module WorldEventSrl =
    let serialiseType (i: WorldEvent.Type): byte[] =

        let label =
            match i with
            | WorldEvent.Moved  _-> 1uy
            | WorldEvent.ObjectAdded _ -> 2uy
            | WorldEvent.ObjectRemoved _ -> 3uy

        let addtInfo =
            match i with
            | WorldEvent.Moved (id, d) ->
                Array.concat [
                    (StringSrl.serialise id)
                    (DirectionSrl.serialise d)
                ]
            | WorldEvent.ObjectAdded obj ->
                WorldObjectSrl.serialise obj
            | WorldEvent.ObjectRemoved id ->
                StringSrl.serialise id

        Array.append [|label|] addtInfo

    let serialise (e: WorldEvent): byte[] =
        let id = StringSrl.serialise e.resultOf
        let payload = serialiseType e.t

        Array.append id payload

    let deserialiseType (bytes: byte[]): WorldEvent.Type DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): WorldEvent.Type DesrlResult =
            match tag with
            | 1uy ->
                DesrlUtil.getTwo
                    StringSrl.deserialise
                    DirectionSrl.deserialise
                    (fun id d -> WorldEvent.Type.Moved (id, d))
                    objectBytes
            | 2uy ->
                WorldObjectSrl.deserialise objectBytes
                |> DesrlResult.map (fun o -> WorldEvent.Type.ObjectAdded o)
            | 3uy ->
                StringSrl.deserialise objectBytes
                |> DesrlResult.map (fun id -> WorldEvent.Type.ObjectRemoved id)
            | _ ->
                printfn "Unknown WorldEvent tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes

    let deserialise (bytes: byte[]): WorldEvent DesrlResult =
        DesrlUtil.getTwo
            StringSrl.deserialise
            deserialiseType
            WorldEvent.asResult
            bytes
