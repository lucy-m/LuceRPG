namespace LuceRPG.Models

module Direction =
    type Model =
        | North
        | East
        | South
        | West

    let toInt (direction: Model): int =
        match direction with
        | North -> 0
        | East -> 1
        | South -> 2
        | West -> 3

    let fromInt (n: int): Model =
        let mod4 =
            let m = n % 4

            // Correction for negative numbers
            if m < 0
            then m + 4
            else m

        match mod4 with
        | 0 -> North
        | 1 -> East
        | 2 -> South
        | _ -> West

    let movePoint (direction: Model) (amount: int) (point: Point): Point =
        let offset =
            match direction with
            | North -> Point.create 0 amount
            | East -> Point.create amount 0
            | South -> Point.create 0 -amount
            | West -> Point.create -amount 0

        Point.add offset point

    let rotateCw (direction: Model): Model =
        match direction with
        | North -> East
        | East -> South
        | South -> West
        | West -> North

    let rotateCwN (turns: int) (direction: Model): Model =
        let n = toInt direction + turns
        fromInt n

    let inverse (direction: Model): Model =
        match direction with
        | North -> South
        | South -> North
        | East -> West
        | West -> East

    let asLetter (d: Model): char =
        match d with
        | North -> 'N'
        | East -> 'E'
        | South -> 'S'
        | West -> 'W'

    let all = set [North; South; East; West]

    /// Sorts points from smallest to largest in given direction
    let sortPoints (direction: Model) (ps: Point seq): Point seq =
        let sortFn: Point -> int =
            match direction with
            | North -> fun p -> p.y
            | South -> fun p -> -p.y
            | East -> fun p -> p.x
            | West -> fun p -> -p.x

        ps
        |> Seq.sortBy sortFn

type Direction = Direction.Model
