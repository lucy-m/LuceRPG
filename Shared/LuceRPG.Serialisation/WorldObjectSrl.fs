namespace LuceRPG.Serialisation

open LuceRPG.Models

module WorldObjectSrl =
    let serialiseType (t: WorldObject.Type): byte[] =
        let label =
            match t with
            | WorldObject.Type.Wall -> 1uy
            | WorldObject.Type.Path _ -> 2uy
            | WorldObject.Type.Player -> 3uy

        let addtInfo =
            match t with
            | WorldObject.Type.Wall -> [||]
            | WorldObject.Type.Path (w,h) ->
                Array.append (IntSrl.serialise w) (IntSrl.serialise h)
            | WorldObject.Type.Player -> [||]

        Array.append [|label|] addtInfo

    let deserialiseType (bytes: byte[]): WorldObject.Type DesrlResult =
        let result =
            Array.tryHead bytes
            |> Option.bind (fun b ->
                match b with
                | 1uy -> DesrlResult.create WorldObject.Type.Wall 1
                | 2uy ->
                    let item =
                        DesrlUtil.getTwo
                            IntSrl.deserialise
                            IntSrl.deserialise
                            (fun w h -> WorldObject.Type.Path (w,h))
                            (Util.safeSkip 1 bytes)
                    DesrlResult.addBytes 1 item
                | 3uy -> DesrlResult.create WorldObject.Type.Player 1
                | _ -> Option.None
            )

        result

    let serialise (obj: WorldObject): byte[] =
        let t = serialiseType (obj.t)
        let topLeft = PointSrl.serialise obj.topLeft

        Array.append t topLeft

    let deserialise (bytes: byte[]): WorldObject DesrlResult =
        DesrlUtil.getTwo
            deserialiseType
            PointSrl.deserialise
            WorldObject.create
            bytes

