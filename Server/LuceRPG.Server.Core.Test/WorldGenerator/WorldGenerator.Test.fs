namespace LuceRPG.Server.Core.WorldGenerator

open NUnit.Framework
open LuceRPG.Models
open FsUnit
open FsCheck

[<TestFixture>]
module WorldGenerator =

    [<TestFixture>]
    module ``for a 4x4 rect world`` =
        let rectWorld: RectWorld =
            let bounds = Rect.create 0 0 4 4
            let paths = [ 0,2; 1,2; 2,2 ] |> Point.toPointSet
            let rectPlots =
                set [
                    set [
                        0,0,4,2
                        3,2,1,1
                        0,3,4,1
                    ]
                    |> Set.map (fun (x,y,w,h) -> Rect.create x y w h)
                ]
            let externals =
                [
                    -1,2, Direction.East
                ]
                |> Point.toPointMap

            {
                bounds = bounds
                paths = paths
                rectPlots = rectPlots
                externals = externals
            }

        let world = WorldGenerator.fromRectWorld "Test" rectWorld

        [<Test>]
        let ``bounds set correctly`` () =

            let expectedBounds =
                set [
                    0,0,8,8
                    -1,4,1,2
                ]
                |> Set.map (fun (x,y,w,h) -> Rect.create x y w h)

            world.bounds |> Set.ofSeq |> should equal expectedBounds

        [<Test>]
        let ``paths created correctly`` () =
            let expectedPaths =
                [
                    0,4
                    2,4
                    4,4
                ]
                |> Point.toPointSet

            let paths =
                world.objects
                |> Map.toSeq
                |> Seq.choose (fun (id, obj) ->
                    match obj.value.t with
                    | WorldObject.Type.Path size -> Option.Some (obj.value.btmLeft, size)
                    | _ -> Option.None
                )

            paths
            |> Seq.map fst
            |> Set.ofSeq
            |> should equal expectedPaths

            paths
            |> Seq.map snd
            |> Set.ofSeq
            |> should equal (set[Point.p2x2])

