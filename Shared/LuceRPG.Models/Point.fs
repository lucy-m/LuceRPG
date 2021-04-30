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

    let scale (s: int) (p: Model): Model =
        {
            x = p.x * s
            y = p.y * s
        }

    let toPointMap (entries: (int * int * 'a) seq): Map<Model, 'a> =
        entries
        |> Seq.map (fun (x,y,e) -> create x y, e)
        |> Map.ofSeq

    let toPointSeq (entries: (int * int) seq): Model seq =
        entries
        |> Seq.map (fun (x,y) -> create x y)

    let toPointSet: (int * int) seq -> Model Set = toPointSeq >> Set.ofSeq

    let p1x1 = create 1 1
    let p2x2 = create 2 2
    let p2x1 = create 2 1
    let p1x2 = create 1 2

type Point = Point.Model
