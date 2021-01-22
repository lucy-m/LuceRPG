namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module Rect =

    [<TestFixture>]
    module contains =

        [<TestFixture>]
        module ``for a 10x2 rect at (5,6)`` =
            let rect = Rect.create 5 6 10 2

            [<Test>]
            let ``left bound correct`` () =
                Rect.leftBound rect |> should equal 5

            [<Test>]
            let ``right bound correct`` () =
                Rect.rightBound rect |> should equal 15

            [<Test>]
            let ``top bound correct`` () =
                Rect.topBound rect |> should equal 6

            [<Test>]
            let ``bottom bound correct`` () =
                Rect.bottomBound rect |> should equal 4

            [<Test>]
            let ``contains correct points`` () =
                let points =
                    [
                        (Point.create 5 6, true)
                        (Point.create 4 6, false)
                        (Point.create 5 7, false)

                        (Point.create 14 6, true)
                        (Point.create 15 6, false)
                        (Point.create 14 7, false)

                        (Point.create 14 5, true)
                        (Point.create 14 4, false)
                        (Point.create 15 5, false)
                    ]

                points
                |> List.map (fun (p, e) ->
                    Rect.contains p rect |> should equal e
                )
                |> ignore
