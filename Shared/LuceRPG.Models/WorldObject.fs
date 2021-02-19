namespace LuceRPG.Models

module WorldObject =
    module Type =
        type Model =
            | Wall
            | Path of int * int
            | Player of PlayerData
            | NPC of PlayerData

    type Type = Type.Model

    type Payload =
        {
            t: Type
            btmLeft: Point
        }

    let t (wo: Payload): Type = wo.t
    let btmLeft (wo: Payload): Point = wo.btmLeft

    let create (t: Type) (btmLeft: Point): Payload =
        {
            t = t
            btmLeft = btmLeft
        }

    let isBlocking (obj: Payload): bool =
        match obj.t with
        | Type.Wall -> true
        | Type.Path _ -> false
        | Type.Player _ -> false
        | Type.NPC _ -> true

    let size (obj: Payload): Point =
        let p2x2 = Point.create 2 2

        match obj.t with
        | Type.Wall -> p2x2
        | Type.Path (w,h) -> Point.create w h
        | Type.Player _ -> p2x2
        | Type.NPC _ -> p2x2

    let getPoints (obj: Payload): Point List =
        let objSize = size obj

        let relPoints =
            ([0 .. (objSize.x - 1)], [0 .. (objSize.y - 1)])
            |> (fun (xs, ys) ->
                xs
                |> List.collect (fun x -> ys |> List.map (fun y -> Point.create x y))
            )

        let blocked =
            relPoints
            |> List.map (fun p1 -> Point.add p1 obj.btmLeft)

        blocked

    let moveObjectN (direction: Direction) (amount: int) (obj: Payload): Payload =
        let newBtmLeft = Direction.movePoint direction amount obj.btmLeft

        {
            obj with
                btmLeft = newBtmLeft
        }

    let moveObject (direction: Direction) (obj: Payload): Payload =
        moveObjectN direction 1 obj

    let atLocation (btmLeft: Point) (obj: Payload): Payload =
        {
            obj with
                btmLeft = btmLeft
        }

    /// Time taken by the object to move one square
    let travelTime (obj: Payload): int64 =
        match obj.t with
        | Type.Player _ -> System.TimeSpan.FromMilliseconds(float(250)).Ticks
        | _ -> 0L

    let isPlayer (obj: Payload): bool =
        match obj.t with
        | Type.Player _ -> true
        | _ -> false

    let getName (obj: Payload): string Option =
        match obj.t with
        | Type.Player pd -> Option.Some pd.name
        | Type.NPC pd -> Option.Some pd.name
        | _ -> Option.None

    type Model = Payload WithId

type WorldObject = WorldObject.Model
