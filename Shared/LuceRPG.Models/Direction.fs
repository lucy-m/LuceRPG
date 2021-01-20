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

type Direction = Direction.Model
