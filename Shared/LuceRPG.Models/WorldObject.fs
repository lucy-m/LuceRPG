namespace LuceRPG.Models

module WorldObject =
    module Type =
        type Model =
            | Wall
            | Path of int * int
            | Player of PlayerData

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
    let id (wo: Model): Id.WorldObject = wo.id

    let isBlocking (obj: Model): bool =
        match obj.value.t with
        | Type.Wall -> true
        | Type.Path _ -> false
        | Type.Player _ -> false

    let size (obj: Payload): Point =
        let p2x2 = Point.create 2 2

        match obj.t with
        | Type.Wall -> p2x2
        | Type.Path (w,h) -> Point.create w h
        | Type.Player _ -> p2x2

    let getPoints (obj: Payload): Point List =
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

    let moveObjectN (direction: Direction) (amount: int) (obj: Model): Model =
        let newTopLeft = Direction.movePoint direction amount obj.value.topLeft

        {
            obj with
                value = {
                    obj.value with
                        topLeft = newTopLeft
                }
        }

    let moveObject (direction: Direction) (obj: Model): Model =
        moveObjectN direction 1 obj

    /// Time taken by the object to move one square
    let travelTime (obj: Payload): int64 =
        match obj.t with
        | Type.Player _ -> System.TimeSpan.FromMilliseconds(float(250)).Ticks
        | _ -> 0L

    let isPlayer (obj: Model): bool =
        match t obj with
        | Type.Player _ -> true
        | _ -> false

type WorldObject = WorldObject.Model
