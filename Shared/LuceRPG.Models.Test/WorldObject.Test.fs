namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module WorldObject =

    [<TestFixture>]
    module getPoints =

        [<TestFixture>]
        module ``1x1 object`` =
            let btmLeft = Point.create 8 2
            let obj = WorldObject.create (WorldObject.Type.Path (1,1)) btmLeft |> TestUtil.withId

            let points = WorldObject.getPoints obj.value

            [<Test>]
            let ``has one point`` () =
                points.Length |> should equal 1

            [<Test>]
            let ``point is btmLeft`` () =
                points.Head |> should equal btmLeft

        [<TestFixture>]
        module ``2x2 object`` =
            let btmLeft = Point.create 10 1
            let obj = WorldObject.create WorldObject.Type.Wall btmLeft |> TestUtil.withId

            let points = WorldObject.getPoints obj.value

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

