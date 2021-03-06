﻿namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module WorldObject =

    [<TestFixture>]
    module getPoints =

        [<TestFixture>]
        module ``1x1 object`` =
            let btmLeft = Point.create 8 2
            let obj = WorldObject.create (WorldObject.Type.Path Point.p1x1) btmLeft Direction.South |> TestUtil.withId

            let points = WorldObject.getPoints obj.value |> List.ofSeq

            [<Test>]
            let ``has one point`` () =
                points.Length |> should equal 1

            [<Test>]
            let ``point is btmLeft`` () =
                points.Head |> should equal btmLeft

        [<TestFixture>]
        module ``2x2 object`` =
            let btmLeft = Point.create 10 1
            let obj = WorldObject.create WorldObject.Type.Wall btmLeft Direction.South |> TestUtil.withId

            let points = WorldObject.getPoints obj.value |> List.ofSeq

            [<Test>]
            let ``has four points`` () =
                points.Length |> should equal 4

            [<Test>]
            let ``has expected points`` () =
                let expected = [
                    btmLeft
                    Point.add btmLeft (Point.create 0 1)
                    Point.add btmLeft (Point.create 1 0)
                    Point.add btmLeft (Point.create 1 1)
                ]

                points |> should be (equivalent expected)

        [<TestFixture>]
        module ``warp`` =
            [<Test>]
            let ``door warp has correct size`` () =
                let warp =
                    Warp.create (Warp.createTarget "" Point.zero) Warp.Appearance.Door
                    |> WorldObject.Type.Warp
                    |> fun t -> WorldObject.create t Point.zero Direction.South

                let size = WorldObject.size warp

                size |> should equal Point.p2x2

            [<Test>]
            let ``mat has correct size`` () =
                let mkWarp =
                    Warp.create (Warp.createTarget "" Point.zero) Warp.Appearance.Mat
                    |> WorldObject.Type.Warp
                    |> fun t -> WorldObject.create t Point.zero

                mkWarp Direction.South |> WorldObject.size |> should equal Point.p2x1
                mkWarp Direction.North |> WorldObject.size |> should equal Point.p2x1
                mkWarp Direction.West |> WorldObject.size |> should equal Point.p1x2
                mkWarp Direction.East |> WorldObject.size |> should equal Point.p1x2

        [<TestFixture>]
        module ``inn`` =

            [<Test>]
            let ``with door has correct points`` () =
                let btmLeft = Point.create 4 3
                let warpData = Option.Some (Warp.createTarget "" Point.zero)
                let obj = WorldObject.create (WorldObject.Type.Inn warpData) btmLeft Direction.South
                let points = WorldObject.getPoints obj |> Set.ofSeq

                let expected =
                    [
                        4,3; 4,4; 4,5;                  // left col
                        9,3; 9,4; 9,5;                  // right col
                        5,4; 5,5; 6,4; 6,5;             // bottom

                        4,6; 5,6; 6,6; 7,6; 8,6; 9,6;   // top
                        4,7; 5,7; 6,7; 7,7; 8,7; 9,7;
                        4,8; 5,8; 6,8; 7,8; 8,8; 9,8;
                        4,9; 5,9; 6,9; 7,9; 8,9; 9,9;
                    ]
                    |> List.map (fun (x,y) -> Point.create x y)
                    |> Set.ofList

                points |> should equal expected

            [<Test>]
            let ``without door has correct points`` () =
                let btmLeft = Point.create 4 3
                let obj = WorldObject.create (WorldObject.Type.Inn Option.None) btmLeft Direction.South
                let points = WorldObject.getPoints obj |> Set.ofSeq

                let expected =
                    [
                        4,3; 4,4; 4,5;                  // left col
                        9,3; 9,4; 9,5;                  // right col

                        5,4; 5,5; 6,4; 6,5;             // bottom
                        7,4; 7,5; 8,4; 8,5;

                        4,6; 5,6; 6,6; 7,6; 8,6; 9,6;   // top
                        4,7; 5,7; 6,7; 7,7; 8,7; 9,7;
                        4,8; 5,8; 6,8; 7,8; 8,8; 9,8;
                        4,9; 5,9; 6,9; 7,9; 8,9; 9,9;
                    ]
                    |> List.map (fun (x,y) -> Point.create x y)
                    |> Set.ofList

                points |> should equal expected
