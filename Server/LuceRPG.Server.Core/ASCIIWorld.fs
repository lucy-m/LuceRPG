namespace LuceRPG.Server.Core

open LuceRPG.Models

module ASCIIWorld =
    let dumpPos (x: int) (y: int) (world: World): string =
        let point = Point.create x y

        if World.pointInBounds point world
        then
            world.blocked
            |> Map.tryFind point
            |> Option.map (fun b ->
                match b with
                | World.BlockedType.Object obj ->
                    match WorldObject.t obj with
                    | WorldObject.Type.Wall _ -> "W"
                    | _ -> " "
                | World.BlockedType.SpawnPoint _ -> "S"
            )
            |> Option.defaultValue " "
        else
            "X"

    let dumpXAxis (minX: int) (maxX: int): string =
        let minXLabel = sprintf "%3i" minX
        let maxXLabel = sprintf "%i" maxX

        let padding = String.replicate (maxX - minX) " "

        minXLabel + padding + maxXLabel

    let dumpRow (minX: int) (maxX: int) (y: int) (world: World): string =
        let xs = [minX .. (maxX - 1)]

        let row =
            xs
            |> List.map (fun x -> dumpPos x y world)
            |> List.reduce (+)

        let yLabel = sprintf "%3i" y

        yLabel + " " + row

    let dump (world: World): string =
        let bounds = world.bounds |> Set.toList

        let minX =
            bounds
            |> List.map Rect.leftBound
            |> List.min

        let maxX =
            bounds
            |> List.map Rect.rightBound
            |> List.max

        let minY =
            bounds
            |> List.map Rect.bottomBound
            |> List.min

        let maxY =
            bounds
            |> List.map Rect.topBound
            |> List.max

        let ys = [minY .. (maxY - 1)] |> List.rev

        let entries =
            ys
            |> List.map (fun y -> dumpRow minX maxX y world)
            |> List.fold (fun acc b -> acc + b + "\r\n") ""

        let xAxis = dumpXAxis minX maxX

        xAxis + "\r\n" + entries
