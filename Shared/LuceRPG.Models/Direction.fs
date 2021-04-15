namespace LuceRPG.Models

module Direction =
    type Model =
        | North
        | South
        | East
        | West

    let movePoint (direction: Model) (amount: int) (point: Point): Point =
        let offset =
            match direction with
            | North -> Point.create 0 amount
            | South -> Point.create 0 -amount
            | East -> Point.create amount 0
            | West -> Point.create -amount 0

        Point.add offset point

    let rotateCw (direction: Model): Model =
        match direction with
        | North -> East
        | East -> South
        | South -> West
        | West -> North

    let rotateCwN (direction: Model) (turns: uint): Model =
        [1u.. (turns % 4u)]
        |> List.fold (fun d _ -> rotateCw d) direction

    let asLetter (d: Model): char =
        match d with
        | North -> 'N'
        | South -> 'S'
        | East -> 'E'
        | West -> 'W'

type Direction = Direction.Model
