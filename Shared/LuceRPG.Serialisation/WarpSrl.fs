namespace LuceRPG.Serialisation

open LuceRPG.Models

module WarpSrl =

    let serialiseTarget (target: Warp.Target): byte[] =
        match target with
        | Warp.Static (worldId, point) ->
            Array.concat
                [
                    [|1uy|]
                    StringSrl.serialise worldId
                    PointSrl.serialise point
                ]

        | Warp.Dynamic (toSeed, direction, index) ->
            Array.concat
                [
                    [|2uy|]
                    IntSrl.serialise toSeed
                    DirectionSrl.serialise direction
                    IntSrl.serialise index
                ]

    let deserialiseTarget (bytes: byte[]): Warp.Target DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): Warp.Target DesrlResult =
            match tag with
            | 1uy ->
                DesrlUtil.getTwo
                    StringSrl.deserialise
                    PointSrl.deserialise
                    (fun worldId point -> Warp.Static (worldId, point))
                    objectBytes
            | 2uy ->
                DesrlUtil.getThree
                    IntSrl.deserialise
                    DirectionSrl.deserialise
                    IntSrl.deserialise
                    (fun toSeed dir index -> Warp.Dynamic (toSeed, dir, index))
                    objectBytes
            | _ ->
                printfn "Unknown WarpTarget tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes

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
