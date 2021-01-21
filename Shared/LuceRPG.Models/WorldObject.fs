namespace LuceRPG.Models

module WorldObject =
    module Type =
        type Model =
            | Wall
            | Path of int * int
            | Player

    type Type = Type.Model

    type Model =
        {
            id: Id.WorldObject
            t: Type
            topLeft: Point
        }

    let create (id: int) (t: Type) (topLeft: Point): Model =
        {
            id = id
            t = t
            topLeft = topLeft
        }

    let isBlocking (obj: Model): bool =
        match obj.t with
        | Type.Wall -> true
        | Type.Path _ -> false
        | Type.Player -> true

    let size (obj: Model): Point =
        match obj.t with
        | Type.Wall -> Point.create 2 2
        | Type.Path (w,h) -> Point.create w h
        | Type.Player -> Point.create 2 2

    let getPoints (obj: Model): Point List =
        let objSize = size obj

        let relPoints =
            ([0 .. (objSize.x - 1)], [0 .. (objSize.y - 1)])
            |> (fun (xs, ys) ->
                xs
                |> List.collect (fun x -> ys |> List.map (fun y -> Point.create x -y))
            )

        let blocked =
            relPoints
            |> List.map (fun p1 -> Point.add p1 obj.topLeft)

        blocked

    let moveObject (direction: Direction) (amount: int) (obj: Model): Model =
        let newTopLeft = Direction.movePoint direction amount obj.topLeft

        {
            obj with
                topLeft = newTopLeft
        }

type WorldObject = WorldObject.Model
