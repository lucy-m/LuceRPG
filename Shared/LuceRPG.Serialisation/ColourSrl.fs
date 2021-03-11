namespace LuceRPG.Serialisation

open LuceRPG.Models

module ColourSrl =
    let serialise ((r, g, b): Colour): byte[] =
        Array.concat [
            ByteSrl.serialise r
            ByteSrl.serialise g
            ByteSrl.serialise b
        ]

    let deserialise (bytes: byte[]): Colour DesrlResult =
        DesrlUtil.getThree
            ByteSrl.deserialise
            ByteSrl.deserialise
            ByteSrl.deserialise
            (fun r g b -> (r, g, b))
            bytes
