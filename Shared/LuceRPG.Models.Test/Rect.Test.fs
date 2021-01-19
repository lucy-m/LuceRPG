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
            let ``contains correct points`` () =
                let points =
                    [
                        (Point.create 5 6, true)
                        (Point.create 4 6, false)
                        (Point.create 5 5, false)

                        (Point.create 14 6, true)
                        (Point.create 15 6, false)
                        (Point.create 14 5, false)

                        (Point.create 14 7, true)
                        (Point.create 14 8, false)
                        (Point.create 15 7, false)
                    ]

                points
                |> List.map (fun (p, e) ->
                    Rect.contains p rect |> should equal e
                )
                |> ignore
