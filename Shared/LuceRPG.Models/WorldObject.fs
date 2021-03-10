namespace LuceRPG.Models

module WorldObject =

    module WarpData =
        type Model =
            {
                toWorld: Id.World
                toPoint: Point
            }

        let create (toWorld: Id.World) (toPoint: Point): Model =
            {
                toWorld = toWorld
                toPoint = toPoint
            }

    type WarpData = WarpData.Model

    module Type =
        type Model =
            | Wall
            | Path of int * int
            | Player of CharacterData
            | NPC of CharacterData
            | Warp of WarpData
            | Tree
            | Inn of WarpData Option

    type Type = Type.Model

    type Payload =
        {
            t: Type
            btmLeft: Point
            facing: Direction
        }

    let t (wo: Payload): Type = wo.t
    let btmLeft (wo: Payload): Point = wo.btmLeft

    let create (t: Type) (btmLeft: Point) (facing: Direction): Payload =
        {
            t = t
            btmLeft = btmLeft
            facing = facing
        }

    let isBlocking (obj: Payload): bool =
        match obj.t with
        | Type.Wall -> true
        | Type.Path _ -> false
        | Type.Player _ -> false
        | Type.NPC _ -> true
        | Type.Warp _ -> false
        | Type.Tree -> true
        | Type.Inn _ -> true

    let size (obj: Payload): Point =
        let p1x1 = Point.create 1 1
        let p2x2 = Point.create 2 2

        match obj.t with
        | Type.Wall -> p2x2
        | Type.Path (w,h) -> Point.create w h
        | Type.Player _ -> p2x2
        | Type.NPC _ -> p2x2
        | Type.Warp _ -> p2x2
        | Type.Tree -> p1x1
        | Type.Inn _ -> Point.create 6 8

    let getPoints (obj: Payload): Point seq =
        match obj.t with
        | Type.Inn door ->
            let relPoints =
                let top = Rect.create 0 3 6 4
                let leftCol = Rect.create 0 0 1 3
                let rightCol = Rect.create 5 0 1 3
                let bottom =
                    if door |> Option.isSome
                    then Rect.create 1 1 2 2
                    else Rect.create 1 1 4 2

                [ top; leftCol; rightCol; bottom ]
                |> Seq.collect Rect.getPoints

            let absPoints =
                relPoints |> Seq.map (Point.add obj.btmLeft)

            absPoints
        | _ ->
            let objSize = size obj
            let rect = Rect.pointCreate obj.btmLeft objSize

            Rect.getPoints rect

    let moveObjectN (direction: Direction) (amount: int) (obj: Payload): Payload =
        let newBtmLeft = Direction.movePoint direction amount obj.btmLeft

        {
            obj with
                btmLeft = newBtmLeft
                facing = direction
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

    let getCharacterData (obj: Payload): CharacterData Option =
        match obj.t with
        | Type.Player cd -> Option.Some cd
        | Type.NPC cd -> Option.Some cd
        | _ -> Option.None

    let getName (obj: Payload): string Option =
        getCharacterData obj
        |> Option.map (fun cd -> cd.name)

    type Model = Payload WithId

type WorldObject = WorldObject.Model
