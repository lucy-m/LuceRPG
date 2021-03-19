namespace LuceRPG.Models

module WorldBackground =
    type Type =
        | Grass
        | Planks

    type Model =
        {
            t: Type
            colour: Colour
        }

    let create (t: Type) (colour: Colour) =
        {
            t = t
            colour = colour
        }

    let GreenGrass = create Type.Grass (86uy, 103uy, 91uy)
    let BrownPlanks = create Type.Planks (94uy, 89uy, 82uy)

type WorldBackground = WorldBackground.Model
