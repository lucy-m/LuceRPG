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
        let tileSet = parameters.tileSet |> Option.defaultValue TileSet.fullUniform

        let pathWorld =
            PathWorld.generateFilled
                parameters.bounds
                tileSet
                random
            |> ExternalCountConstraint.constrainAll parameters.eccs random

        let plotWorld = PlotWorld.generateGrouped pathWorld
        let rectWorld = RectWorld.divide random plotWorld

        let id = sprintf "Generated-%i" seed

        fromRectWorld id rectWorld |> WithId.useId id

