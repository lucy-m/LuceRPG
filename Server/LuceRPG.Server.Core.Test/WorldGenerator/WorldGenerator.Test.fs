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

    [<Test>]
    let ``same seed generated same world`` () =
        let parameters: WorldGenerator.Parameters =
            {
                bounds = Rect.create 0 0 8 8
                eccs = Map.empty
                tileSet = Option.None
            }

        let checkProps: (World.Payload -> obj) List =
            [
                fun w -> w.background :> obj
                fun w -> w.blocked :> obj
                fun w -> w.bounds :> obj
                fun w -> w.interactions :> obj
                fun w -> w.name :> obj
                fun w -> w.playerSpawner :> obj
                fun w -> w.warps :> obj

                // Object IDs will be generated differently
                fun w ->
                    w.objects
                    |> Map.toSeq
                    |> Seq.map (fun (id, obj) -> obj.value)
                    |> Set.ofSeq :> obj
            ]

        let checkFn (seed: int): bool =
            let g1 = WorldGenerator.generate parameters seed
            let g2 = WorldGenerator.generate parameters seed

            let w1 = (fst g1).value
            let w2 = (fst g2).value

            let pass =
                let failures =
                    checkProps
                    |> List.map (fun fn -> fn, (fn w1).Equals(fn w2))
                    |> List.filter (fun (fn, pass) -> not pass)

                failures |> List.isEmpty

            pass

        Check.QuickThrowOnFailure checkFn
