namespace LuceRPG.Models

module Rect =
    type Model = {
        btmLeft: Point
        size: Point
    }

    let create (x: int) (y: int) (w: int) (h: int): Model =
        let size =
            Point.create
                (max 0 w)
                (max 0 h)

        {
            btmLeft = Point.create x y
            size = size
        }

    let pointCreate (btmLeft: Point) (size: Point): Model =
        {
            btmLeft = btmLeft
            size = size
        }

    let scale (s: int) (rect: Model): Model =
        {
            btmLeft = Point.scale s rect.btmLeft
            size = Point.scale s rect.size
        }

    /// inclusive left bound
    let leftBound (rect: Model): int =
        rect.btmLeft.x

    /// exclusive right bound
    let rightBound (rect: Model): int =
        rect.btmLeft.x + rect.size.x

    /// exclusive top bound
    let topBound (rect: Model): int =
        rect.btmLeft.y + rect.size.y

    /// inclusive bottom bound
    let bottomBound (rect: Model): int =
        rect.btmLeft.y

    let contains (p: Point) (rect: Model): bool =
        let inX = p.x >= (leftBound rect) && p.x < (rightBound rect)
        let inY = p.y >= (bottomBound rect) && p.y < (topBound rect)

        inX && inY

    let xs (rect: Model): int seq =
        [leftBound rect .. rightBound rect - 1]
        |> Seq.ofList

    let ys (rect: Model): int seq =
        [bottomBound rect .. topBound rect - 1]
        |> Seq.ofList

    let getPoints (rect: Model): Point seq =
        let xs = xs rect
        let ys = ys rect

        let points =
            xs
            |> Seq.collect (fun x ->
                ys
                |> Seq.map (fun y -> Point.create x y)
            )

        points

    let debugString (rect: Model) (mapFn: Point -> char): string =
        let xs = xs rect
        let ys = ys rect |> Seq.rev

        ys
        |> Seq.map (fun y ->
            xs
            |> Seq.map (fun x ->
                let point = Point.create x y
                mapFn point
            )
            |> Array.ofSeq
            |> System.String
            |> fun s -> s + "\n"
        )
        |> Seq.reduce (+)

type Rect = Rect.Model
