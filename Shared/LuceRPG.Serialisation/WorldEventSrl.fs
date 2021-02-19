namespace LuceRPG.Serialisation

open LuceRPG.Models

module WorldEventSrl =
    let serialiseType (i: WorldEvent.Type): byte[] =

        let label =
            match i with
            | WorldEvent.Moved  _-> 1uy
            | WorldEvent.ObjectAdded _ -> 2uy
            | WorldEvent.ObjectRemoved _ -> 3uy
            | WorldEvent.JoinedWorld _ -> 4uy

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
            | WorldEvent.JoinedWorld cId ->
                StringSrl.serialise cId

        Array.append [|label|] addtInfo

    let serialise (e: WorldEvent): byte[] =
        let id = StringSrl.serialise e.resultOf
        let worldId = StringSrl.serialise e.world
        let index = IntSrl.serialise e.index
        let payload = serialiseType e.t

        Array.concat [id; worldId; index; payload]

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
                |> DesrlResult.map WorldEvent.Type.ObjectAdded
            | 3uy ->
                StringSrl.deserialise objectBytes
                |> DesrlResult.map WorldEvent.Type.ObjectRemoved
            | 4uy ->
                StringSrl.deserialise objectBytes
                |> DesrlResult.map WorldEvent.Type.JoinedWorld
            | _ ->
                printfn "Unknown WorldEvent tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes

    let deserialise (bytes: byte[]): WorldEvent DesrlResult =
        DesrlUtil.getFour
            StringSrl.deserialise
            StringSrl.deserialise
            IntSrl.deserialise
            deserialiseType
            WorldEvent.asResult
            bytes
