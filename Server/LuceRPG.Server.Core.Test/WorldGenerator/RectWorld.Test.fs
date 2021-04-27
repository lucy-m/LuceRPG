namespace LuceRPG.Server.Core.WorldGenerator

open NUnit.Framework
open LuceRPG.Models
open FsUnit
open FsCheck

[<TestFixture>]
module RectWorld =
    [<Test>]
    let ``divides an L-shaped plot into two`` () =
        let plot: Plot =
            [
                0,2; 1,2
                0,1; 1,1
                0,0; 1,0; 2,0
            ]
            |> Point.toPointSet

        // solutions
        // 11       11
        // 11       11
        // 112      222
        let sln1 =
            [ 0, 0, 2, 3; 2, 0, 1, 1 ]
            |> Seq.map (fun (x, y, w, h) -> Rect.create x y w h)
            |> Set.ofSeq

        let sln2 =
            [ 0, 0, 3, 1; 0, 1, 2, 2 ]
            |> Seq.map (fun (x, y, w, h) -> Rect.create x y w h)
            |> Set.ofSeq

        let checkFn (): bool =
            let random = System.Random()
            let divided = RectWorld.dividePlot random plot

            divided = sln1 || divided = sln2

        Check.QuickThrowOnFailure checkFn

    [<Test>]
    let ``all plots are assigned to a rect`` () =
        let checkFn (): bool =
            let random = System.Random()
            let bounds = Rect.create 0 0 10 10
            let tileMap = [5, 5, Tile.cross] |> Point.toPointMap

            let pathWorld =
                PathWorld.create tileMap Map.empty bounds
                |> PathWorld.fill TileSet.fullUniform random

            let plotWorld = PlotWorld.generateGrouped pathWorld

            let rectWorld = RectWorld.divide random plotWorld

            let plotWorldPoints =
                plotWorld.groupedPlots
                |> Seq.collect id
                |> Set.ofSeq

            let rectWorldPoints =
                rectWorld.rectPlots
                |> Seq.collect id
                |> Seq.collect Rect.getPoints
                |> Set.ofSeq

            plotWorldPoints = rectWorldPoints

        Check.QuickThrowOnFailure checkFn
