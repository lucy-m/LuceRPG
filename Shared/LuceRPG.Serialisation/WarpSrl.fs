namespace LuceRPG.Serialisation

open LuceRPG.Models

module WarpSrl =
    let serialiseTarget (target: Warp.Target): byte[] =
        Array.append
            (StringSrl.serialise target.toWorld)
            (PointSrl.serialise target.toPoint)

    let deserialiseTarget (bytes: byte[]): Warp.Target DesrlResult =
        DesrlUtil.getTwo
            StringSrl.deserialise
            PointSrl.deserialise
            Warp.createTarget
            bytes

    let serialiseAppearance (a: Warp.Appearance): byte[] =
        match a with
        | Warp.Appearance.Door -> [|1uy|]
        | Warp.Appearance.Mat -> [|2uy|]

    let deserialiseAppearance (bytes: byte[]): Warp.Appearance DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): Warp.Appearance DesrlResult =
            match tag with
            | 1uy -> DesrlResult.create Warp.Appearance.Door 0
            | 2uy -> DesrlResult.create Warp.Appearance.Mat 0
            | _ ->
                printfn "Unknown WarpAppearance tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes

    let serialise (w: Warp): byte[] =
        Array.append
            (serialiseTarget w.target)
            (serialiseAppearance w.appearance)

    let deserialise (bytes: byte[]): Warp DesrlResult =
        DesrlUtil.getTwo
            deserialiseTarget
            deserialiseAppearance
            Warp.create
            bytes
