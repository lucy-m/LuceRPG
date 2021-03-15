namespace LuceRPG.Models

module Flower =
    let stemsCount = 4uy
    let headsCount = 4uy

    module HeadColour =
        let pink: Colour = (255uy, 155uy, 255uy)
        let aqua: Colour = (101uy, 175uy, 183uy)
        let sunshine: Colour = (255uy, 175uy, 93uy)
        let rose: Colour = (255uy, 107uy, 111uy)

    type Model =
        {
            stem: byte
            head: byte
            headColour: Colour
        }

    let create
            (stem: byte)
            (head: byte)
            (headColour: Colour)
            : Model =
        {
            stem = stem
            head = head
            headColour = headColour
        }

    let randomized (): Model =
        let r = System.Random()

        let stem = (System.BitConverter.GetBytes(r.Next()) |> Array.head) % stemsCount
        let head = (System.BitConverter.GetBytes(r.Next()) |> Array.head) % headsCount

        let headColour =
            let n = r.Next()
            let count = 4

            match n with
            | x when x % count = 0 -> HeadColour.pink
            | x when x % count = 1 -> HeadColour.aqua
            | x when x % count = 2 -> HeadColour.sunshine
            | _ -> HeadColour.rose

        create stem head headColour

type Flower = Flower.Model
