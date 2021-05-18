namespace LuceRPG.Server.Core.WorldGenerator

open LuceRPG.Models
open LuceRPG.Server.Core

module WorldGenerator =
    type ExistingWarp =
        {
            fromWorld: Id.World
            returnPoints: Point seq
        }

    type Parameters =
        {
            bounds: Rect
            /// Direction should be the direction FROM new map
            existingWarps: Map<Direction, ExistingWarp>
        }

    /// Creates a World from the RectWorld
    /// This will scale the world by 2x
    /// Fills in paths but not warps
    let fromRectWorld (name: string) (rectWorld: RectWorld): World.Payload =
        let dynamicWarps =
            rectWorld.externals
            |> Map.toSeq
            |> Seq.map (fun (p, d) ->
                let offset =
                    match d with
                    | Direction.North | Direction.East -> Point.zero
                    | Direction.South -> Point.create 0 1
                    | Direction.West -> Point.create 1 0

                let origin =
                    Point.scale 2 p
                    |> Point.add offset

                origin, d
            )
            |> Map.ofSeq

        let bounds =
            dynamicWarps
            |> Map.toSeq
            |> Seq.map (fun (origin, d) ->
                let size =
                    match d with
                    | Direction.North | Direction.South -> Point.p2x1
                    | Direction.East | Direction.West -> Point.p1x2
                Rect.pointCreate origin size
            )
            |> Seq.append ([ Rect.scale 2 rectWorld.bounds ])

        let pathPoints = rectWorld.paths |> Set.map (Point.scale 2)
        let spawn = pathPoints |> Seq.tryHead |> Option.defaultValue Point.zero
        let background = WorldBackground.GreenGrass

        let paths =
            pathPoints
            |> Set.map (fun p ->
                WorldObject.create (WorldObject.Type.Path Point.p2x2) p Direction.South
                |> WithId.create
            )
            |> Set.toSeq

        World.createWithObjs
            name
            bounds
            spawn
            background
            paths
        |> World.withDynamicWarps dynamicWarps

    let generate (parameters: Parameters) (seed: int): World =
        let random = System.Random(seed)
        let tileSet = TileSet.fullUniform

        let eccs =
            parameters.existingWarps
            |> Map.map (fun d ew -> ew.returnPoints |> Seq.length |> ExternalCountConstraint.Exactly)

        let pathWorld =
            PathWorld.generateFilled
                parameters.bounds
                tileSet
                random
            |> ExternalCountConstraint.constrainAll eccs random

        let plotWorld = PlotWorld.generateGrouped pathWorld
        let rectWorld = RectWorld.divide random plotWorld

        let id = sprintf "Generated-%i" seed

        let warpless = fromRectWorld id rectWorld

        // For the existing warps, want to create a static warp back to
        //   the given world
        let staticWarps =
            parameters.existingWarps
            |> Map.toSeq
            |> Seq.collect (fun (outDir, ew) ->
                // Find all valid warp points from the generated map
                let warpLocations =
                    warpless.dynamicWarps
                    |> Map.filter (fun p warpDir -> warpDir = outDir)
                    |> Map.toSeq
                    |> Seq.map fst

                let sortByX =
                    match outDir with
                    | Direction.North | Direction.South -> true
                    | Direction.East | Direction.West -> false

                let sortPoints (ps: Point seq) =
                    ps
                    |> Seq.sortBy (fun p -> if sortByX then p.x else p.y)

                Seq.zip
                    (sortPoints warpLocations)
                    (sortPoints ew.returnPoints)
                |> Seq.map (fun (warpPoint, toPoint) ->
                    warpPoint, toPoint, outDir, ew.fromWorld
                )
            )
            |> Seq.map (fun (warpPoint, toPoint, outDir, toWorldId) ->
                Warp.createTarget toWorldId toPoint
                |> fun t -> Warp.create t Warp.Mat
                |> WorldObject.Type.Warp
                |> fun t -> WorldObject.create t warpPoint outDir
                |> WithId.create
            )

        // Create dynamic warps objects for all other warp points in this map
        let dynamicWarpObjects =
            Direction.all
            |> Seq.filter (fun d -> parameters.existingWarps |> Map.containsKey d |> not)
            |> Seq.collect (fun outDir ->
                let seed = random.Next()

                let warpLocations =
                    warpless.dynamicWarps
                    |> Map.filter (fun p warpDir -> warpDir = outDir)
                    |> Map.toSeq
                    |> Seq.map fst
                    |> Seq.sortBy (fun p ->
                        match outDir with
                        | Direction.North | Direction.South -> p.x
                        | Direction.East | Direction.West -> p.y
                    )
                    |> Seq.indexed

                warpLocations
                |> Seq.map (fun (i,p) ->
                    Warp.Dynamic (seed, outDir, i)
                    |> fun t -> Warp.create t Warp.Mat
                    |> WorldObject.Type.Warp
                    |> fun t -> WorldObject.create t p outDir
                    |> WithId.create
                )
            )

        let warpObjects = Seq.append staticWarps dynamicWarpObjects

        let warpful = World.addObjects warpObjects warpless

        warpful |> WithId.useId id

