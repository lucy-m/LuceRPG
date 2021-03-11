namespace LuceRPG.Models

module CharacterData =
    type HairStyle =
        | Short
        | Long
        | Egg

    module HairColour =
        let banana: Colour = (255uy, 232uy, 117uy)
        let cocoa85: Colour = (59uy, 37uy, 19uy)
        let tangerine: Colour = (242uy, 145uy, 65uy)
        let caramel: Colour = (173uy, 120uy, 55uy)

    module SkinColour =
        let ivory: Colour = (252uy, 232uy, 204uy)
        let butter: Colour = (240uy, 211uy, 158uy)
        let mocha: Colour = (196uy, 155uy, 108uy)
        let cocoa70: Colour = (110uy, 76uy, 48uy)

    module ClothingColour =
        let grey: Colour = (160uy, 159uy, 166uy)
        let maroon: Colour = (130uy, 62uy, 69uy)
        let navy: Colour = (47uy, 57uy, 97uy)
        let grass: Colour = (122uy, 179uy, 100uy)
        let sky: Colour = (156uy, 219uy, 210uy)

    type Model =
        {
            name: string
            hairStyle: HairStyle
            hairColour: Colour
            skinColour: Colour
            topColour: Colour
            bottomColour: Colour
        }

    let create
            (hairStyle: HairStyle)
            (hairColour: Colour)
            (skinColour: Colour)
            (topColour: Colour)
            (bottomColour: Colour)
            (name: string)
            : Model =
        {
            name = name
            hairStyle = hairStyle
            hairColour = hairColour
            skinColour = skinColour
            topColour = topColour
            bottomColour = bottomColour
        }

    let randomized (name: string): Model =
        let r = System.Random()
        let hairStyle =
            let n = r.Next()
            let count = 3

            match n with
            | x when x % count = 0 -> HairStyle.Short
            | x when x % count = 1 -> HairStyle.Long
            | _ -> HairStyle.Egg

        let hairColour =
            let n = r.Next()
            let count = 4

            match n with
            | x when x % count = 0 -> HairColour.banana
            | x when x % count = 1 -> HairColour.cocoa85
            | x when x % count = 2 -> HairColour.tangerine
            | _ -> HairColour.caramel

        let skinColour =
            let n = r.Next()
            let count = 4

            match n with
            | x when x % count = 0 -> SkinColour.ivory
            | x when x % count = 1 -> SkinColour.butter
            | x when x % count = 2 -> SkinColour.mocha
            | _ -> SkinColour.cocoa70

        let topColour, bottomColour =
            let clothingColour () =
                let n = r.Next()
                let count = 5

                match n with
                | x when x % count = 0 -> ClothingColour.grey
                | x when x % count = 1 -> ClothingColour.maroon
                | x when x % count = 2 -> ClothingColour.navy
                | x when x % count = 3 -> ClothingColour.grass
                | _ -> ClothingColour.sky

            clothingColour(), clothingColour()

        create hairStyle hairColour skinColour topColour bottomColour name

type CharacterData = CharacterData.Model
