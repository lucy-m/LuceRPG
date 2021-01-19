namespace LuceRPG.Serialisation

open LuceRPG.Models

module WorldObjectSrl =
    let serialiseType (t: WorldObject.Type): byte[] =
        let byte =
            match t with
            | WorldObject.Type.Wall -> 1uy
            | WorldObject.Type.Path -> 2uy

        [|byte|]

    let deserialiseType (bytes: byte[]): WorldObject.Type DesrlResult =
        let value =
            Array.tryHead bytes
            |> Option.bind (fun b ->
                match b with
                | 1uy -> Option.Some WorldObject.Type.Wall
                | 2uy -> Option.Some WorldObject.Type.Path
                | _ -> Option.None
            )
        DesrlResult.bind value 1

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

