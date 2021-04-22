namespace LuceRPG.Server.Core.WorldGenerator

open LuceRPG.Models

// Expands a TileWorld into map 3x the size and
//   fills in blank spots with plots
module PlotWorld =
    type Model =
        {
            bounds: Rect
            paths: Point Set
            groupedPlots: Point Set Set
            externals: Map<Point, Direction>
        }

    type Ungrouped =
        {
            bounds: Rect
            paths: Point Set
            plots: Point Set
            externals: Map<Point, Direction>
        }

    let debugPrint (model: Model): string =
        let xs = [Rect.leftBound model.bounds .. Rect.rightBound model.bounds - 1]
        let ys =
            [Rect.bottomBound model.bounds .. Rect.topBound model.bounds - 1]
            |> List.rev

        let pointByPlotGroup =
            model.groupedPlots
            |> Seq.indexed
            |> Seq.fold (fun map (i, set) ->
                set
                |> Seq.fold (fun map p ->
                    Map.add p i map
                ) map
            ) Map.empty

        ys
        |> Seq.map (fun y ->
            xs
            |> Seq.map (fun x ->
                let point = Point.create x y

                if model.paths |> Set.contains point
                then '▓'
                else
                    pointByPlotGroup
                    |> Map.tryFind point
                    |> Option.map (fun i -> char(i + int 'A'))
                    |> Option.defaultValue ' '
            )
            |> Array.ofSeq
            |> System.String
            |> fun s -> s + "\n"
        )
        |> Seq.reduce (+)

    let tileToPaths (tile: Tile) (pathWorldPosition: Point): Point Set =
        [
            Direction.North, 1, 2
            Direction.East,  2, 1
            Direction.South, 1, 0
            Direction.West,  0, 1
        ]
        |> Seq.choose (fun (d, x, y) ->
            if tile |> Set.contains d
            then Option.Some (Point.create x y)
            else Option.None
        )
        |> Seq.fold (fun set p ->
            Set.add p set
        ) Set.empty
        |> Set.add Point.p1x1
        |> Set.map (fun p ->
            Point.scale 3 pathWorldPosition
            |> Point.add p
        )

    let convertExternal (external: Point * Direction): Point * Direction =
        let offset =
            match snd external with
            | Direction.North -> 1, 2
            | Direction.East  -> 2, 1
            | Direction.South -> 1, 0
            | Direction.West -> 0, 1
            |> fun (x, y) -> Point.create x y

        let origin = Point.scale 3 (fst external)
        let position = Point.add offset origin

        position, snd external

    let generateUngrouped (pathWorld: PathWorld): Ungrouped =
        let bounds =
            Rect.create
                pathWorld.bounds.btmLeft.x
                pathWorld.bounds.btmLeft.y
                (pathWorld.bounds.size.x * 3)
                (pathWorld.bounds.size.y * 3)

        let paths =
            pathWorld.tileMap
            |> Map.toSeq
            |> Seq.map (fun (p, t) -> tileToPaths t p)
            |> Seq.fold Set.union Set.empty

        let externals =
            pathWorld.externalMap
            |> Map.toSeq
            |> Seq.map convertExternal
            |> Seq.fold (fun acc n -> Map.add (fst n) (snd n) acc) Map.empty

        let plots =
            Rect.getPoints bounds
            |> Seq.filter (fun p -> Set.contains p paths |> not)
            |> Set.ofSeq

        {
            bounds = bounds
            paths = paths
            plots = plots
            externals = externals
        }

    /// Groups together adjacent plots
    let group (ungroupedWorld: Ungrouped): Model =

        let rec groupInner
                (ungrouped: Point Set)
                (currentTile: Point)
                (looking: Direction)
                (thisGroup: Point Set)
                (thisGroupUnchecked: Point Set)
                (allGroups: Point Set Set)
                : Model =

            // Check if there is a plot in the looking direction
            let check = Direction.movePoint looking 1 currentTile

            let thisGroup, thisGroupUnchecked, ungrouped =
                if ungrouped |> Set.contains check
                then
                    Set.add check thisGroup,
                    Set.add check thisGroupUnchecked,
                    Set.remove check ungrouped
                else
                    thisGroup, thisGroupUnchecked, ungrouped

            // Check the next direction
            let nextDir = Direction.rotateCw looking
            if nextDir = Direction.North
            then
                // We are back at north so check the next tile
                thisGroupUnchecked
                |> Seq.tryHead
                |> Option.map (fun nextTile ->
                    // There are tiles left to check in this group
                    let thisGroupUnchecked = thisGroupUnchecked |> Set.remove nextTile
                    let ungrouped = ungrouped |> Set.remove nextTile

                    groupInner
                        ungrouped
                        nextTile
                        nextDir
                        thisGroup
                        thisGroupUnchecked
                        allGroups
                )
                |> Option.defaultWith (fun () ->
                    // This group is complete
                    let allGroups = allGroups |> Set.add thisGroup

                    ungrouped
                    |> Seq.tryHead
                    |> Option.map (fun nextTile ->
                        // Start a new group
                        let ungrouped = ungrouped |> Set.remove nextTile
                        let thisGroup = nextTile |> Set.singleton

                        groupInner
                            ungrouped
                            nextTile
                            nextDir
                            thisGroup
                            Set.empty
                            allGroups
                    )
                    |> Option.defaultWith (fun () ->
                        // We have no ungrouped tiles remaining
                        let allGroups = allGroups |> Set.add thisGroup

                        let model: Model =
                            {
                                bounds = ungroupedWorld.bounds
                                paths = ungroupedWorld.paths
                                groupedPlots = allGroups
                                externals = ungroupedWorld.externals
                            }

                        model
                    )
                )
            else
                groupInner
                    ungrouped
                    currentTile
                    nextDir
                    thisGroup
                    thisGroupUnchecked
                    allGroups

        ungroupedWorld.plots
        |> Seq.tryHead
        |> Option.map (fun firstTile ->
            let ungrouped = ungroupedWorld.plots |> Set.remove firstTile
            let thisGroup = firstTile |> Set.singleton

            groupInner
                ungrouped
                firstTile
                Direction.North
                thisGroup
                Set.empty
                Set.empty
        )
        |> Option.defaultWith (fun () ->
            // There are no plots in this world
            let model: Model =
                {
                    bounds = ungroupedWorld.bounds
                    paths = ungroupedWorld.paths
                    groupedPlots = Set.empty
                    externals = ungroupedWorld.externals
                }

            model
        )

    let generateGrouped = generateUngrouped >> group
