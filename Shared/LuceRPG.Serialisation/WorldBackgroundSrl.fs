namespace LuceRPG.Serialisation

open LuceRPG.Models

module WorldBackgroundSrl =
    let serialise (w: WorldBackground): byte[] =
        let t =
            match w.t with
            | WorldBackground.Grass -> 1uy
            | WorldBackground.Planks -> 2uy

        let colour = ColourSrl.serialise w.colour

        Array.concat [ [|t|]; colour]

    let deserialise (bytes: byte[]): WorldBackground DesrlResult =
        let loadT (tag: byte) (objectBytes: byte[]): WorldBackground.Type DesrlResult =
            match tag with
            | 1uy -> DesrlResult.create WorldBackground.Type.Grass 0
            | 2uy -> DesrlResult.create WorldBackground.Type.Planks 0
            | _ ->
                printfn "Unknown WorldBackground tile tag %u" tag
                Option.None

        DesrlUtil.getTwo
            (DesrlUtil.getTagged loadT)
            ColourSrl.deserialise
            WorldBackground.create
            bytes

