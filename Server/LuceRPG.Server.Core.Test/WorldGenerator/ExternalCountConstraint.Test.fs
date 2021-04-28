namespace LuceRPG.Server.Core.WorldGenerator

open NUnit.Framework
open LuceRPG.Models
open FsUnit
open FsCheck

[<TestFixture>]
module ExternalCountConstraint =

    [<TestFixture>]
    module ``for model with 3 south externals`` =
        let bounds = Rect.create 0 1 4 4
        let externals =
            [ 0; 1; 2 ]
            |> List.map (fun x -> Point.create x 0, Direction.North)
            |> Map.ofList

        let pathWorld = PathWorld.create Map.empty externals bounds

        [<Test>]
        let ``Exactly 2 has RemoveExternal result`` () =
            let c = ExternalCountConstraint.Exactly 2
            ExternalCountConstraint.check pathWorld Direction.South c
            |> should be (ofCase <@ExternalCountConstraint.Result.RemoveExternal@>)

        [<Test>]
        let ``Exactly 3 has Satisfied result`` () =
            let c = ExternalCountConstraint.Exactly 3
            ExternalCountConstraint.check pathWorld Direction.South c
            |> should be (ofCase <@ExternalCountConstraint.Result.Satisfied@>)

        [<Test>]
        let ``Exactly 4 has AddExternal result`` () =
            let c = ExternalCountConstraint.Exactly 4
            ExternalCountConstraint.check pathWorld Direction.South c
            |> should be (ofCase <@ExternalCountConstraint.Result.AddExternal@>)

        [<Test>]
        let ``Exactly 8 has Unsatisfiable result`` () =
            let c = ExternalCountConstraint.Exactly 8
            ExternalCountConstraint.check pathWorld Direction.South c
            |> should be (ofCase <@ExternalCountConstraint.Result.Unsatisfiable@>)

        [<Test>]
        let ``Between 2 and 4 is satisfied`` () =
            let c = ExternalCountConstraint.Between (2, 4)
            ExternalCountConstraint.check pathWorld Direction.South c
            |> should be (ofCase <@ExternalCountConstraint.Result.Satisfied@>)

        [<Test>]
        let ``Between 4 and 2 is satisfied`` () =
            let c = ExternalCountConstraint.Between (4, 2)
            ExternalCountConstraint.check pathWorld Direction.South c
            |> should be (ofCase <@ExternalCountConstraint.Result.Satisfied@>)

        [<Test>]
        let ``Between 3 and 4 is satisfied`` () =
            let c = ExternalCountConstraint.Between (3, 4)
            ExternalCountConstraint.check pathWorld Direction.South c
            |> should be (ofCase <@ExternalCountConstraint.Result.Satisfied@>)

        [<Test>]
        let ``Between 4 and 7 is AddExternal`` () =
            let c = ExternalCountConstraint.Between (4, 7)
            ExternalCountConstraint.check pathWorld Direction.South c
            |> should be (ofCase <@ExternalCountConstraint.Result.AddExternal@>)

        [<Test>]
        let ``Between 1 and 3 is RemoveExternal`` () =
            let c = ExternalCountConstraint.Between (1, 3)
            ExternalCountConstraint.check pathWorld Direction.South c
            |> should be (ofCase <@ExternalCountConstraint.Result.RemoveExternal@>)

        [<Test>]
        let ``Between 5 and 7 is Unsatisfiable`` () =
            let c = ExternalCountConstraint.Between (5, 7)
            ExternalCountConstraint.check pathWorld Direction.South c
            |> should be (ofCase <@ExternalCountConstraint.Result.Unsatisfiable@>)

        [<Test>]
        let ``Between -5 and 2 is RemoveExternal`` () =
            let c = ExternalCountConstraint.Between (-5, 2)
            ExternalCountConstraint.check pathWorld Direction.South c
            |> should be (ofCase <@ExternalCountConstraint.Result.RemoveExternal@>)

        [<Test>]
        let ``Between -5 and 0 is Unsatisfiable`` () =
            let c = ExternalCountConstraint.Between (-5, 0)
            ExternalCountConstraint.check pathWorld Direction.South c
            |> should be (ofCase <@ExternalCountConstraint.Result.Unsatisfiable@>)

        [<Test>]
        let ``Between is symmetric`` () =
            let checkFn (n1: int) (n2: int): bool =
                let c1 = ExternalCountConstraint.Between (n1, n2)
                let c2 = ExternalCountConstraint.Between (n2, n1)

                ExternalCountConstraint.check pathWorld Direction.South c1
                    = ExternalCountConstraint.check pathWorld Direction.South c2

            Check.QuickThrowOnFailure checkFn

    [<TestFixture>]
    module ``constrainOnEdge`` =
        let random = System.Random()

        [<TestFixture>]
        module ``simple 2x2 L`` =
            // Initial
            //
            // ╻
            // ┗━x

            let bounds = Rect.create 0 0 2 2
            let tileMap =
                [
                    0, 0, Tile.LNE
                    1, 0, Tile.straightEW
                    0, 1, Tile.deadEndS
                ]
                |> Point.toPointMap

            let externals = [ Point.create 2 0, Direction.West] |> Map.ofList

            let initial = PathWorld.create tileMap externals bounds

            [<Test>]
            let ``Exactly 1 North constraint is correct`` () =
                // Initial      Expected
                //               x
                // ╻             ┃
                // ┗━x           ┗━x

                let constrained =
                    ExternalCountConstraint.constrainOnEdge
                        Direction.North
                        (ExternalCountConstraint.Exactly 1)
                        random
                        initial

                let expectedTileMap =
                    tileMap
                    |> Map.add (Point.create 0 1) Tile.straightNS

                let expectedExternals =
                    externals
                    |> Map.add (Point.create 0 2) Direction.South

                constrained |> Option.isSome |> should equal true
                constrained.Value.tileMap |> should equal expectedTileMap
                constrained.Value.externalMap |> should equal expectedExternals

            [<Test>]
            let ``Exactly 2 North constraint is correct`` () =
                // Initial      Expected
                //               xx
                // ╻             ┃┃
                // ┗━x           ┗┻x

                let constrained =
                    ExternalCountConstraint.constrainOnEdge
                        Direction.North
                        (ExternalCountConstraint.Exactly 2)
                        random
                        initial

                let expectedTileMap =
                    tileMap
                    |> Map.add (Point.create 0 1) Tile.straightNS
                    |> Map.add (Point.create 1 0) Tile.TWNE
                    |> Map.add (Point.create 1 1) Tile.straightNS

                let expectedExternals =
                    externals
                    |> Map.add (Point.create 0 2) Direction.South
                    |> Map.add (Point.create 1 2) Direction.South

                constrained |> Option.isSome |> should equal true
                constrained.Value.tileMap |> should equal expectedTileMap
                constrained.Value.externalMap |> should equal expectedExternals

            [<Test>]
            let ``Exactly 3 North constraint is Unsatisfiable`` () =
                let constrained =
                    ExternalCountConstraint.constrainOnEdge
                        Direction.North
                        (ExternalCountConstraint.Exactly 3)
                        random
                        initial

                constrained |> Option.isNone |> should equal true

            [<Test>]
            let ``Exactly 0 North constraint does nothing`` () =
                let constrained =
                    ExternalCountConstraint.constrainOnEdge
                        Direction.North
                        (ExternalCountConstraint.Exactly 0)
                        random
                        initial

                constrained |> Option.isSome |> should equal true
                constrained.Value |> should equal initial

            [<Test>]
            let ``Exactly 0 East constraint is correct`` () =
                // Initial      Expected
                //
                // ╻             ╻
                // ┗━x           ┗╸

                let constrained =
                    ExternalCountConstraint.constrainOnEdge
                        Direction.East
                        (ExternalCountConstraint.Exactly 0)
                        random
                        initial

                let expectedTileMap =
                    tileMap
                    |> Map.add (Point.create 1 0) Tile.deadEndW

                constrained |> Option.isSome |> should equal true
                constrained.Value.tileMap |> should equal expectedTileMap
                constrained.Value.externalMap |> should equal Map.empty

        [<TestFixture>]
        module ``for a 5x5 map`` =
            // Map
            //  _____
            // |
            // |
            // | ┏━┓
            // |╺┛ ┃
            // |   ┃
            //     x

            let bounds = Rect.create 0 0 5 5
            let tileMap =
                [
                    0, 1, Tile.deadEndE
                    1, 1, Tile.LWN
                    1, 2, Tile.LES
                    2, 2, Tile.straightEW
                    3, 2, Tile.LSW
                    3, 1, Tile.straightNS
                    3, 0, Tile.straightNS
                ]
                |> Point.toPointMap

            let externalMap = [ 3, -1, Direction.North ] |> Point.toPointMap

            let initial = PathWorld.create tileMap externalMap bounds

            [<Test>]
            let ``adding west constraints always adds in same order`` () =
                let checkFn (): bool =
                    let random = System.Random()

                    let first =
                        ExternalCountConstraint.constrainOnEdge
                            Direction.West
                            (ExternalCountConstraint.Exactly 1)
                            random
                            initial

                    let expectedTileMap =
                        tileMap
                        |> Map.add (Point.create 0 1) Tile.straightEW

                    let expectedExternals =
                        externalMap
                        |> Map.add (Point.create -1 1) Direction.East

                    first |> Option.isSome |> should equal true
                    first.Value.tileMap |> should equal expectedTileMap
                    first.Value.externalMap |> should equal expectedExternals

                    let second =
                        ExternalCountConstraint.constrainOnEdge
                            Direction.West
                            (ExternalCountConstraint.Exactly 2)
                            random
                            first.Value

                    let expectedTileMap =
                        tileMap
                        |> Map.add (Point.create 0 1) Tile.straightEW
                        |> Map.add (Point.create 0 2) Tile.straightEW
                        |> Map.add (Point.create 1 2) Tile.TESW

                    let expectedExternals =
                        externalMap
                        |> Map.add (Point.create -1 1) Direction.East
                        |> Map.add (Point.create -1 2) Direction.East

                    second |> Option.isSome |> should equal true
                    second.Value.tileMap |> should equal expectedTileMap
                    second.Value.externalMap |> should equal expectedExternals

                    true

                Check.QuickThrowOnFailure checkFn

            [<Test>]
            let ``same seed produces same result`` () =
                let checkFn (seed: int): bool =
                    let r1 = System.Random(seed)
                    let r2 = System.Random(seed)

                    let v1, v2 =
                        let make (random: System.Random) =
                            ExternalCountConstraint.constrainOnEdge
                                Direction.North
                                (ExternalCountConstraint.Exactly 2)
                                random
                                initial

                        make r1, make r2

                    v1 = v2

                Check.QuickThrowOnFailure checkFn

            [<Test>]
            let ``produces correct number of externals`` () =
                let checkFn (): bool =
                    let r = System.Random()
                    let targetCount = r.Next(0, 4)

                    let result =
                        ExternalCountConstraint.constrainOnEdge
                            Direction.South
                            (ExternalCountConstraint.Exactly targetCount)
                            r
                            initial

                    result |> Option.isSome |> should equal true

                    let southExternals =
                        result.Value.externalMap
                        |> Map.filter (fun p d -> d = Direction.North)
                        |> Map.count

                    targetCount = southExternals

                Check.QuickThrowOnFailure checkFn
