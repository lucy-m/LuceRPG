namespace LuceRPG.Models

module Rect =
    type Model = {
        topLeft: Point
        size: Point
    }

    let create (x: int) (y: int) (w: int) (h: int): Model =
        let size =
            Point.create
                (max 0 w)
                (max 0 h)

        {
            topLeft = Point.create x y
            size = size
        }

    let pointCreate (topLeft: Point) (size: Point): Model =
        {
            topLeft = topLeft
            size = size
        }

    /// inclusive left bound
    let leftBound (rect: Model): int =
        rect.topLeft.x

    /// exclusive right bound
    let rightBound (rect: Model): int =
        rect.topLeft.x + rect.size.x

    /// inclusive top bound
    let topBound (rect: Model): int =
        rect.topLeft.y

    /// exclusive bottom bound
    let bottomBound (rect: Model): int =
        rect.topLeft.y + rect.size.y

    let contains (p: Point) (rect: Model): bool =
        let inX = p.x >= (leftBound rect) && p.x < (rightBound rect)
        let inY = p.y >= (topBound rect) && p.y < (bottomBound rect)

        inX && inY

type Rect = Rect.Model
