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
            eccs: Map<Direction, ExternalCountConstraint>
            tileSet: TileSet Option
        }

    /// Creates a World from the RectWorld
    /// This will scale the world by 2x
    let fromRectWorld (name: string) (rectWorld: RectWorld): World.Payload =
        let bounds =
            rectWorld.externals
            |> Map.toSeq
            |> Seq.map (fun (p, d) ->
                let offset =
                    match d with
                    | Direction.East -> Point.create 1 0
                    | Direction.North -> Point.create 0 1
                    | Direction.West | Direction.South -> Point.zero

                let origin =
                    Point.scale 2 p
                    |> Point.add offset

                let size =
                    match d with
                    | Direction.North | Direction.South -> Point.p2x1
                    | Direction.East | Direction.West -> Point.p1x2
                Rect.pointCreate origin size
            )
            |> Seq.append ([ Rect.scale 2 rectWorld.bounds ])

        let dynamicWarps =
            rectWorld.externals
            |> Map.map (fun p d -> Direction.inverse d)

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

    let generate (parameters: Parameters) (seed: int): World * Map<Point, Direction> =
        let random = System.Random(seed)
        let tileSet = parameters.tileSet |> Option.defaultValue TileSet.fullUniform

        let pathWorld =
            PathWorld.generateFilled
                parameters.bounds
                tileSet
                random
            |> ExternalCountConstraint.constrainAll parameters.eccs random

        let plotWorld = PlotWorld.generateGrouped pathWorld
        let rectWorld = RectWorld.divide random plotWorld

        let spawnPoints =
            rectWorld.externals
            |> Map.toSeq
            |> Seq.map (fun (p, d) ->
                let moved = Direction.movePoint d 1 p
                let scaled = Point.scale 2 moved

                scaled, d
            )
            |> Map.ofSeq

        let id = sprintf "Generated-%i" seed

        fromRectWorld id rectWorld |> WithId.useId id, spawnPoints

