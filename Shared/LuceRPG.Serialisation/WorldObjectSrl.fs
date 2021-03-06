namespace LuceRPG.Serialisation

open LuceRPG.Models

module WorldObjectSrl =
    let serialiseType (t: WorldObject.Type): byte[] =
        let label =
            match t with
            | WorldObject.Type.Wall -> 1uy
            | WorldObject.Type.Path _ -> 2uy
            | WorldObject.Type.Player _ -> 3uy
            | WorldObject.Type.NPC _ -> 4uy
            | WorldObject.Type.Warp _ -> 5uy

        let addtInfo =
            match t with
            | WorldObject.Type.Wall -> [||]
            | WorldObject.Type.Path (w,h) ->
                Array.append (IntSrl.serialise w) (IntSrl.serialise h)
            | WorldObject.Type.Player d -> CharacterDataSrl.serialise d
            | WorldObject.Type.NPC d -> CharacterDataSrl.serialise d
            | WorldObject.Type.Warp (worldId, point) ->
                Array.append (StringSrl.serialise worldId) (PointSrl.serialise point)

        Array.append [|label|] addtInfo

    let deserialiseType (bytes: byte[]): WorldObject.Type DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): WorldObject.Type DesrlResult =
            match tag with
            | 1uy -> DesrlResult.create WorldObject.Type.Wall 0
            | 2uy ->
                DesrlUtil.getTwo
                    IntSrl.deserialise
                    IntSrl.deserialise
                    (fun w h -> WorldObject.Type.Path (w,h))
                    objectBytes
            | 3uy ->
                CharacterDataSrl.deserialise objectBytes
                |> DesrlResult.map (fun d -> WorldObject.Type.Player d)
            | 4uy ->
                CharacterDataSrl.deserialise objectBytes
                |> DesrlResult.map (fun d -> WorldObject.Type.NPC d)
            | 5uy ->
                DesrlUtil.getTwo
                    StringSrl.deserialise
                    PointSrl.deserialise
                    (fun worldId point -> WorldObject.Type.Warp(worldId, point))
                    objectBytes
            | _ ->
                printfn "Unknown WorldObject Type tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes

    let serialisePayload (obj: WorldObject.Payload): byte[] =
        let t = serialiseType obj.t
        let btmLeft = PointSrl.serialise obj.btmLeft
        let facing = DirectionSrl.serialise obj.facing

        Array.concat  [t; btmLeft; facing]

    let deserialisePayload (bytes: byte[]): WorldObject.Payload DesrlResult =
        DesrlUtil.getThree
            deserialiseType
            PointSrl.deserialise
            DirectionSrl.deserialise
            WorldObject.create
            bytes

    let serialise (obj: WorldObject): byte[] =
        WithIdSrl.serialise serialisePayload obj

    let deserialise (bytes: byte[]): WorldObject DesrlResult =
        WithIdSrl.deserialise deserialisePayload bytes
