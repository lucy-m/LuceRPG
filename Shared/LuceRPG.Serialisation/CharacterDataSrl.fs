namespace LuceRPG.Serialisation

open LuceRPG.Models

module CharacterDataSrl =
    let serialiseColour ((r, g, b): CharacterData.Colour): byte[] =
        Array.concat [
            ByteSrl.serialise r
            ByteSrl.serialise g
            ByteSrl.serialise b
        ]

    let serialise (d: CharacterData): byte[] =
        let name = StringSrl.serialise d.name

        let hairStyle =
            match d.hairStyle with
            | CharacterData.HairStyle.Short -> 1uy
            | CharacterData.HairStyle.Long -> 2uy
            | CharacterData.HairStyle.Egg -> 3uy

        Array.concat [
            [|hairStyle|]
            (serialiseColour d.hairColour)
            (serialiseColour d.skinColour)
            (serialiseColour d.topColour)
            (serialiseColour d.bottomColour)
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

    let deserialiseColour (bytes: byte[]): CharacterData.Colour DesrlResult =
        DesrlUtil.getThree
            ByteSrl.deserialise
            ByteSrl.deserialise
            ByteSrl.deserialise
            (fun r g b -> (r, g, b))
            bytes

    let deserialise (bytes: byte[]): CharacterData DesrlResult =
        DesrlUtil.getSix
            deserialiseHairStyle
            deserialiseColour
            deserialiseColour
            deserialiseColour
            deserialiseColour
            StringSrl.deserialise
            CharacterData.create
            bytes
