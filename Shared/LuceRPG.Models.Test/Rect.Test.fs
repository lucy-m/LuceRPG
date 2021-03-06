﻿namespace LuceRPG.Models

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
                Rect.topBound rect |> should equal 8

            [<Test>]
            let ``bottom bound correct`` () =
                Rect.bottomBound rect |> should equal 6

            [<Test>]
            let ``contains correct points`` () =
                let points =
                    [
                        (Point.create 5 6, true)    // btm left
                        (Point.create 5 7, true)    // N
                        (Point.create 5 8, false)   // NN
                        (Point.create 6 6, true)    // E
                        (Point.create 5 5, false)   // S
                        (Point.create 4 6, false)   // W

                        (Point.create 14 6, true)   // btm right
                        (Point.create 14 7, true)   // N
                        (Point.create 14 8, false)  // NN
                        (Point.create 15 6, false)  // E
                        (Point.create 14 5, false)  // S
                        (Point.create 13 6, true)   // W
                    ]

                points
                |> List.map (fun (p, e) ->
                    Rect.contains p rect |> should equal e
                )
                |> ignore

            [<Test>]
            let ``getPoints gives correct points`` () =
                let points =
                    Rect.getPoints rect
                    |> Set.ofSeq

                let expected =
                    [
                        5,6; 6,6; 7,6; 8,6; 9,6; 10,6; 11,6; 12,6; 13,6; 14,6;
                        5,7; 6,7; 7,7; 8,7; 9,7; 10,7; 11,7; 12,7; 13,7; 14,7;
                    ]
                    |> List.map (fun (x,y) -> Point.create x y)
                    |> Set.ofList

                points |> should equal expected

    [<TestFixture>]
    module ``debugString`` =
        [<Test>]
        let ``correct`` () =
            let rect = Rect.create 0 0 5 5

            let mapFn (point: Point): char =
                char(point.x + point.y + int '0')

            let result = Rect.debugString rect mapFn

            let expected =
                "45678\n" +
                "34567\n" +
                "23456\n" +
                "12345\n" +
                "01234\n"

            result |> should equal expected

