namespace LuceRPG.Server.Core.WorldGenerator

open LuceRPG.Models
open LuceRPG.Server.Core

type RectPlot = Rect Set

module RectWorld =
    type Model =
        {
            bounds: Rect
            paths: Point Set
            rectPlots: RectPlot Set
            externals: Map<Point, Direction>
        }

    let debugString (model: Model): string =
        let rectPlotsByPoint =
            model.rectPlots
            |> Seq.collect id
            |> Seq.indexed
            |> Seq.fold (fun acc (i, r) ->
                Rect.getPoints r
                |> Seq.fold (fun acc p -> acc |> Map.add p (i,r)) acc
            ) Map.empty

        let mapFn (point: Point): char =
            if model.paths |> Set.contains point
            then '▓'
            else
                rectPlotsByPoint
                |> Map.tryFind point
                |> Option.map (fun (i,r) ->
                    match point with
                    | x when x = r.btmLeft -> char(i + int 'A')
                    | p when p.x = r.btmLeft.x -> '|'
                    | p when p.y = r.btmLeft.y -> '_'
                    | _ -> ' '
                )
                |> Option.defaultValue ' '

        Rect.debugString model.bounds mapFn

    /// Divides a given plot into rectangles
    let dividePlot (random: System.Random) (plot: Plot): RectPlot =

        // Valid seed points are points without
        // South and West neighbours
        let getSeedPoint (plot: Plot): Point Option =
            let validSeeds =
                plot
                |> Set.filter (fun p ->
                    let southNeighbour = Direction.movePoint Direction.South 1 p
                    let westNeighbour = Direction.movePoint Direction.West 1 p

                    not (Set.contains southNeighbour plot)
                    && not (Set.contains westNeighbour plot)
                )

            Util.randomOf random validSeeds

        let rec dividePlotInner
                (current: Rect)
                (forceDirection: Direction Option)
                (unused: Point Set)
                (divided: Rect Set)
                : Rect Set =

            let expansionDirection =
                forceDirection
                |> Option.defaultWith (fun () ->
                    if random.Next() % 2 = 0
                    then Direction.North
                    else Direction.East
                )

            // Try to expand this rectangle in that direction
            let requiredPoints =
                match expansionDirection with
                | Direction.North ->
                    // Requires an unused point for all xs in the
                    // top bound of current rect
                    let xs = [Rect.leftBound current .. Rect.rightBound current - 1]
                    let y = Rect.topBound current

                    xs |> Seq.map (fun x -> Point.create x y) |> Set.ofSeq

                | _ ->
                    // Requires an unused point for all ys in the
                    // right bound of current rect
                    let x = Rect.rightBound current
                    let ys = [Rect.bottomBound current .. Rect.topBound current - 1]

                    ys |> Seq.map (fun y -> Point.create x y) |> Set.ofSeq

            let hasRequired =
                requiredPoints
                |> Seq.map (fun p -> unused |> Set.contains p)
                |> Seq.reduce (&&)

            if hasRequired
            then
                // Expand this rectangle
                let sizeChange =
                    match expansionDirection with
                    | Direction.North -> Point.create 0 1
                    | _ -> Point.create 1 0

                let current = Rect.pointCreate current.btmLeft (Point.add current.size sizeChange)

                let unused = Set.difference unused requiredPoints

                dividePlotInner current Option.None unused divided

            else
                // This rectangle cannot be expanded in this direction
                forceDirection
                |> Option.map (fun fd ->
                    // This rectangle cannot be expanded further
                    let divided = divided |> Set.add current

                    getSeedPoint unused
                    |> Option.map (fun seedPoint ->
                        // Move on to the next rectangle
                        let current = Rect.pointCreate seedPoint Point.p1x1
                        let forceDirection = Option.None
                        let unused = unused |> Set.remove seedPoint

                        dividePlotInner current forceDirection unused divided
                    )
                    |> Option.defaultWith (fun () ->
                        // We have finished dividing this plot
                        divided
                    )
                )
                |> Option.defaultWith (fun () ->
                    // Try to expand this rectangle in the opposite direction
                    let forceDirection =
                        match expansionDirection with
                        | Direction.North -> Direction.East
                        | _  -> Direction.North
                        |> Option.Some

                    dividePlotInner current forceDirection unused divided
                )

        getSeedPoint plot
        |> Option.map (fun seed ->
            let current = Rect.pointCreate seed Point.p1x1
            let unused = plot |> Set.remove seed

            dividePlotInner current Option.None unused Set.empty
        )
        |> Option.defaultValue Set.empty

    let divide (random: System.Random) (plotWorld: PlotWorld): Model =
        let rectPlots =
            plotWorld.groupedPlots
            |> Set.map (dividePlot random)

        {
            bounds = plotWorld.bounds
            paths = plotWorld.paths
            rectPlots = rectPlots
            externals = plotWorld.externals
        }
