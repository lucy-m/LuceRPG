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

    let getPoints (rect: Model): Point seq =
        let xs = [leftBound rect .. rightBound rect - 1]
        let ys = [bottomBound rect .. topBound rect - 1]

        let points =
            xs
            |> Seq.collect (fun x ->
                ys
                |> Seq.map (fun y -> Point.create x y)
            )

        points

type Rect = Rect.Model
