﻿namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module WorldObject =

    [<TestFixture>]
    module getPoints =

        [<TestFixture>]
        module ``1x1 object`` =
            let topLeft = Point.create 8 2
            let obj = WorldObject.create (WorldObject.Type.Path (1,1)) topLeft |> TestUtil.withId

            let points = WorldObject.getPoints obj.value

            [<Test>]
            let ``has one point`` () =
                points.Length |> should equal 1

            [<Test>]
            let ``point is topLeft`` () =
                points.Head |> should equal topLeft

        [<TestFixture>]
        module ``2x2 object`` =
            let topLeft = Point.create 10 1
            let obj = WorldObject.create WorldObject.Type.Wall topLeft |> TestUtil.withId

            let points = WorldObject.getPoints obj.value

            [<Test>]
            let ``has four points`` () =
                points.Length |> should equal 4

            [<Test>]
            let ``has expected points`` () =
                let expected = [
                    topLeft
                    Point.add topLeft (Point.create 0 -1)
                    Point.add topLeft (Point.create 1 0)
                    Point.add topLeft (Point.create 1 -1)
                ]

                points |> should be (equivalent expected)

