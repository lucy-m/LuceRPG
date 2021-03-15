namespace LuceRPG.Serialisation

open LuceRPG.Models

module FlowerSrl =
    let serialise (flower: Flower): byte[] =
        Array.concat [
            ByteSrl.serialise flower.stem
            ByteSrl.serialise flower.head
            ColourSrl.serialise flower.headColour
        ]

    let deserialise (bytes: byte[]): Flower DesrlResult =
        DesrlUtil.getThree
            ByteSrl.deserialise
            ByteSrl.deserialise
            ColourSrl.deserialise
            Flower.create
            bytes
