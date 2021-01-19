namespace LuceRPG.Models

module Point =
    type Model = {
        x: int
        y: int
    }

    let create (x: int) (y: int): Model =
        {
            x = x
            y = y
        }

    let zero = create 0 0

    let add (p1: Model) (p2: Model): Model =
        {
            x = p1.x + p2.x
            y = p1.y + p2.y
        }

type Point = Point.Model
