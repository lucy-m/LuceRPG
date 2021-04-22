namespace LuceRPG.Server.Core.WorldGenerator

open NUnit.Framework
open LuceRPG.Models
open FsUnit
open FsCheck

[<TestFixture>]
module PlotWorld =

    [<Test>]
    let ``tileToPaths correct`` () =
        let tile = Tile.LES
        let point = Point.create 2 1    // Translates to 6, 3 in PlotWorld

        let expected =
            [
                7, 4    // center
                7, 3    // south
                8, 4    // east
            ]
            |> Point.toPointSet

        let paths = PlotWorld.tileToPaths tile point

        paths |> should equal expected

    [<Test>]
    let ``convertExternal correct for North`` () =
        let point = Point.create 2 1    // Translates to 6, 3 in PlotWorld
        let direction = Direction.North

        let expected = Point.create 7 5, direction
        let actual = PlotWorld.convertExternal (point, direction)

        actual |> should equal expected

    [<TestFixture>]
    module ``for a simple 3x3 world`` =
        // World
        //  ___
        // | ╻
        // x━╋┓
        // | ┗┻x

        let pathWorld =
            let bounds = Rect.create 0 0 3 3

            let tileMap =
                [
                    0, 1, Tile.straightEW
                    1, 1, Tile.cross
                    1, 2, Tile.deadEndS
                    2, 1, Tile.LSW
                    1, 0, Tile.LNE
                    2, 0, Tile.TWNE
                ]
                |> Point.toPointMap

            let externals =
                [
                    -1, 1, Direction.East
                    3, 0, Direction.West
                ]
                |> Point.toPointMap

            PathWorld.create tileMap externals bounds

        [<Test>]
        let ``generateUngrouped correct`` () =
            let plotWorld = PlotWorld.generateUngrouped pathWorld

            let bounds = Rect.create 0 0 9 9

            let paths =
                [                           // original
                    0,4; 1,4; 2,4           // 0,1
                    3,4; 4,4; 5,4; 4,5; 4,3 // 1,1
                    4,6; 4,7                // 1,2
                    6,4; 7,4; 7,3           // 2,1
                    4,2; 4,1; 5,1           // 1,0
                    7,2; 6,1; 7,1; 8,1      // 2,0
                ]
                |> Point.toPointSet

            let externals =
                [
                    -1, 4, Direction.East
                    9,  1, Direction.West
                ]
                |> Point.toPointMap

            let expectedPlotCount =
                plotWorld.bounds.size.x * plotWorld.bounds.size.y - plotWorld.paths.Count

            plotWorld.bounds |> should equal bounds
            plotWorld.paths |> should equal paths
            plotWorld.externals |> should equal externals
            plotWorld.plots |> Set.count |> should equal expectedPlotCount

        [<Test>]
        let ``generateGrouped correct`` () =
            let ungrouped = PlotWorld.generateUngrouped pathWorld
            let plotWorld = PlotWorld.group ungrouped
            let dbg = PlotWorld.debugPrint plotWorld

            plotWorld.groupedPlots |> Set.count |> should equal 3

            let totalPlotCount =
                plotWorld.groupedPlots
                |> Seq.map (fun g -> g |> Set.count)
                |> Seq.reduce (+)

            totalPlotCount |> should equal ungrouped.plots.Count

    [<Test>]
    let ``foo`` () =
        let random = System.Random ()
        let bounds = Rect.create 0 0 6 6
        let external = [ -1, 5, Direction.East ] |> Point.toPointMap
        let tileSet = TileSet.fullUniform

        let initial = PathWorld.create Map.empty external bounds

        let plotWorld =
            PathWorld.fill tileSet random initial
            |> PathWorld.fillInDirection tileSet Direction.East random
            |> PlotWorld.generateGrouped

        let debugString = PlotWorld.debugPrint plotWorld

        true |> should equal true
