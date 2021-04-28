namespace LuceRPG.Models

module Warp =
    type Appearance = Door | Mat

    type Target =
        | Static of Id.World * Point
        | Dynamic of int * Direction

    let createTarget (toWorld: Id.World) (toPoint: Point): Target = Static (toWorld, toPoint)

    type Model =
        {
            target: Target
            appearance: Appearance
        }

    let create (target: Target) (appearance: Appearance): Model =
        {
            target = target
            appearance = appearance
        }

    let size (warp: Model) (facing: Direction): Point =
        match warp.appearance with
        | Door -> Point.p2x2
        | Mat ->
            match facing with
            | Direction.North
            | Direction.South -> Point.p2x1
            | Direction.West
            | Direction.East -> Point.p1x2

type Warp = Warp.Model
