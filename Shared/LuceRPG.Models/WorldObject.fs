﻿namespace LuceRPG.Models

module WorldObject =
    module Type =
        type Model =
            | Wall
            | Path

    type Type = Type.Model

    type Model =
        {
            t: Type
            topLeft: Point
        }

    let create (t: Type) (topLeft: Point): Model =
        {
            t = t
            topLeft = topLeft
        }

    let isBlocking (obj: Model): bool =
        match obj.t with
        | Type.Wall -> true
        | Type.Path -> false

    let size (obj: Model): Point =
        match obj.t with
        | Type.Wall -> Point.create 2 2
        | Type.Path -> Point.create 1 1

    let getPoints (obj: Model): Point List =
        let objSize = size obj

        let relPoints =
            ([0 .. (objSize.x - 1)], [0 .. (objSize.y - 1)])
            |> (fun (xs, ys) ->
                xs
                |> List.collect (fun x -> ys |> List.map (fun y -> Point.create x y))
            )

        let blocked =
            relPoints
            |> List.map (fun p1 -> Point.add p1 obj.topLeft)

        blocked

type WorldObject = WorldObject.Model
