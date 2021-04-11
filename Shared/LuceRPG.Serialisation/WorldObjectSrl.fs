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
            | WorldObject.Type.Tree -> 6uy
            | WorldObject.Type.Inn _ -> 7uy
            | WorldObject.Type.Flower _ -> 8uy

        let addtInfo =
            match t with
            | WorldObject.Type.Wall -> [||]
            | WorldObject.Type.Path s -> PointSrl.serialise s
            | WorldObject.Type.Player d -> CharacterDataSrl.serialise d
            | WorldObject.Type.NPC d -> CharacterDataSrl.serialise d
            | WorldObject.Type.Warp wd -> WarpSrl.serialise wd
            | WorldObject.Type.Tree -> [||]
            | WorldObject.Type.Inn doorWarp ->
                OptionSrl.serialise
                    WarpSrl.serialiseTarget
                    doorWarp
            | WorldObject.Type.Flower f -> FlowerSrl.serialise f

        Array.append [|label|] addtInfo

    let deserialiseType (bytes: byte[]): WorldObject.Type DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): WorldObject.Type DesrlResult =
            match tag with
            | 1uy -> DesrlResult.create WorldObject.Type.Wall 0
            | 2uy ->
                PointSrl.deserialise objectBytes
                |> DesrlResult.map WorldObject.Type.Path
            | 3uy ->
                CharacterDataSrl.deserialise objectBytes
                |> DesrlResult.map WorldObject.Type.Player
            | 4uy ->
                CharacterDataSrl.deserialise objectBytes
                |> DesrlResult.map WorldObject.Type.NPC
            | 5uy ->
                WarpSrl.deserialise objectBytes
                |> DesrlResult.map WorldObject.Type.Warp
            | 6uy -> DesrlResult.create WorldObject.Type.Tree 0
            | 7uy ->
                OptionSrl.deserialise
                    WarpSrl.deserialiseTarget
                    objectBytes
                |> DesrlResult.map WorldObject.Type.Inn
            | 8uy ->
                FlowerSrl.deserialise objectBytes
                |> DesrlResult.map WorldObject.Type.Flower
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
