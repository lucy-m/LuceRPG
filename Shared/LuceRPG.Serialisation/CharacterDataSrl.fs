namespace LuceRPG.Serialisation

open LuceRPG.Models

module CharacterDataSrl =
    let serialise (d: CharacterData): byte[] =
        let name = StringSrl.serialise d.name

        let hairStyle =
            match d.hairStyle with
            | CharacterData.HairStyle.Short -> 1uy
            | CharacterData.HairStyle.Long -> 2uy
            | CharacterData.HairStyle.Egg -> 3uy

        Array.concat [
            [|hairStyle|]
            (ColourSrl.serialise d.hairColour)
            (ColourSrl.serialise d.skinColour)
            (ColourSrl.serialise d.topColour)
            (ColourSrl.serialise d.bottomColour)
            name
        ]

    let deserialiseHairStyle (bytes: byte[]): CharacterData.HairStyle DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): CharacterData.HairStyle DesrlResult =
            match tag with
            | 1uy -> DesrlResult.create CharacterData.HairStyle.Short 0
            | 2uy -> DesrlResult.create CharacterData.HairStyle.Long 0
            | 3uy -> DesrlResult.create CharacterData.HairStyle.Egg 0
            | _ ->
                printfn "Unknown HairStyle tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes

    let deserialise (bytes: byte[]): CharacterData DesrlResult =
        DesrlUtil.getSix
            deserialiseHairStyle
            ColourSrl.deserialise
            ColourSrl.deserialise
            ColourSrl.deserialise
            ColourSrl.deserialise
            StringSrl.deserialise
            CharacterData.create
            bytes
