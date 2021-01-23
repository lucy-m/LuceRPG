namespace LuceRPG.Models

module WorldObject =
    module Type =
        type Model =
            | Wall
            | Path of int * int
            | Player
            | PlayerSpawner

    type Type = Type.Model

    type Payload =
        {
            t: Type
            topLeft: Point
        }

    type Model = Payload WithId

    let create (t: Type) (topLeft: Point): Payload =
        {
            t = t
            topLeft = topLeft
        }

    let topLeft (wo: Model): Point = wo.value.topLeft
    let t (wo: Model): Type = wo.value.t

    let isBlocking (obj: Model): bool =
        match obj.value.t with
        | Type.Wall -> true
        | Type.Path _ -> false
        | Type.Player -> false
        | Type.PlayerSpawner -> true

    let size (obj: Model): Point =
        let p2x2 = Point.create 2 2

        match obj.value.t with
        | Type.Wall -> p2x2
        | Type.Path (w,h) -> Point.create w h
        | Type.Player -> p2x2
        | Type.PlayerSpawner -> p2x2

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
            |> List.map (fun p1 -> Point.add p1 obj.value.topLeft)

        blocked

    let moveObject (direction: Direction) (amount: int) (obj: Model): Model =
        let newTopLeft = Direction.movePoint direction amount obj.value.topLeft

        {
            obj with
                value = {
                    obj.value with
                        topLeft = newTopLeft
                }
        }

type WorldObject = WorldObject.Model
