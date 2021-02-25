namespace LuceRPG.Models

module CharacterData =
    type HairStyle =
        | Short
        | Long
        | Egg

    type Colour = byte * byte * byte

    module HairColour =
        let banana: Colour = (255uy, 232uy, 117uy)
        let cocoa85: Colour = (59uy, 37uy, 19uy)
        let tangerine: Colour = (242uy, 145uy, 65uy)
        let caramel: Colour = (173uy, 120uy, 55uy)

    type Model =
        {
            name: string
            hairStyle: HairStyle
            hairColour: Colour
        }

    let create
            (hairStyle: HairStyle)
            (hairColour: Colour)
            (name: string)
            : Model =
        {
            name = name
            hairStyle = hairStyle
            hairColour = hairColour
        }

    let randomized (name: string): Model =
        let r = System.Random()
        let hairStyle =
            let n = r.Next()

            match n with
            | x when x % 3 = 0 -> HairStyle.Short
            | x when x % 3 = 1 -> HairStyle.Long
            | _ -> HairStyle.Egg

        let hairColour =
            let n = r.Next()

            match n with
            | x when x % 4 = 0 -> HairColour.banana
            | x when x % 4 = 1 -> HairColour.cocoa85
            | x when x % 4 = 2 -> HairColour.tangerine
            | _ -> HairColour.caramel

        create hairStyle hairColour name

type CharacterData = CharacterData.Model
