﻿namespace LuceRPG.Server.Core.WorldGenerator

open NUnit.Framework
open LuceRPG.Models
open FsUnit
open FsCheck

[<TestFixture>]
module PathWorld =

    [<TestFixture>]
    module ``getLinks`` =
        [<Test>]
        let ``correct for 2x2 world with no warps`` () =
            let bounds = Rect.create 0 0 2 2
            let btmLeft = Point.zero
            let btmRight = Point.create 1 0

            let tileMap =
                [
                    btmLeft, Tile.LNE
                    btmRight, Tile.straightEW
                ]
                |> Map.ofList

            let model = PathWorld.create tileMap Map.empty bounds

            let withLinks = PathWorld.getLinks model

            let expected =
                [
                    btmLeft, set [Direction.North]
                    btmRight, set [Direction.East]
                ]
                |> Map.ofList

            withLinks.activeLinks |> should equal expected

        [<Test>]
        let ``links to external tiles are not active`` () =
            let bounds = Rect.create 0 0 2 2
            let btmLeft = Point.zero
            let btmRight = Point.create 1 0

            let tileMap =
                [
                    btmLeft, Tile.LNE
                    btmRight, Tile.straightEW   // This east link connects to warp
                ]
                |> Map.ofList

            let external =
                [
                    Direction.movePoint Direction.East 1 btmRight, set [Direction.West]
                ]
                |> Map.ofList

            let model = PathWorld.create tileMap external bounds

            let withLinks = PathWorld.getLinks model

            let expected =
                [
                    btmLeft, set [Direction.North]
                ]
                |> Map.ofList

            withLinks.activeLinks |> should equal expected

        [<Test>]
        let ``external tiles without links are active`` () =
            let bounds = Rect.create 0 0 2 2
            let externalPoint = Point.create -1 0
            let external =
                [
                    externalPoint, set [Direction.East]
                ]
                |> Map.ofList

            let model = PathWorld.create Map.empty external bounds

            let withLinks = PathWorld.getLinks model
            let expected = external

            withLinks.activeLinks |> should equal expected

    [<TestFixture>]
    module ``addTile`` =
        let random = System.Random()

        [<TestFixture>]
        module ``tile with all directions surrounded`` =
            let bounds = Rect.create 0 0 3 3
            let centerPoint = Point.p1x1

            let south = Point.create 1 0
            let east = Point.create 2 1
            let north = Point.create 1 2
            let west = Point.create 0 1

            let se = Point.create 2 0
            let ne = Point.create 2 2
            let nw = Point.create 0 2
            let sw = Point.zero

            [<TestFixture>]
            module ``with south link open`` =
                let tileMap =
                    [
                        south, Tile.TWNE            // this link is open
                        east,  Tile.straightNS
                        north, Tile.straightEW
                        west,  Tile.straightNS

                        se, Tile.LWN
                        ne, Tile.LSW
                        nw, Tile.LES
                        sw, Tile.LNE
                    ]
                    |> Map.ofList

                let model = PathWorld.create tileMap Map.empty bounds

                let withLinks = PathWorld.getLinks model

                [<Test>]
                let ``links are correct`` () =
                    let expected =
                        [
                            south, set [Direction.North]
                        ]
                        |> Map.ofList

                    withLinks.activeLinks |> should equal expected

                [<Test>]
                let ``adding a tile from the full set adds a dead-end piece`` () =
                    let tileSet = TileSet.fullUniform

                    let addedTile =
                        PathWorld.addTile
                            random
                            tileSet
                            centerPoint
                            withLinks

                    addedTile |> Option.isSome |> should equal true
                    addedTile.Value.model.tileMap |> Map.containsKey Point.p1x1 |> should equal true
                    addedTile.Value.model.tileMap |> Map.find Point.p1x1 |> should equal Tile.deadEndS

                    addedTile.Value.activeLinks |> Map.isEmpty |> should equal true

        [<TestFixture>]
        module ``tile next to boundary`` =
            let bounds = Rect.create 0 0 1 1
            let tileSet = TileSet.create [ Tile.deadEndE, 1u, set [0] ]

            let model = PathWorld.create Map.empty Map.empty bounds

            let withLinks = PathWorld.getLinks model

            [<Test>]
            let ``links are correct`` () =
                withLinks.activeLinks |> Map.isEmpty |> should equal true

            [<Test>]
            let ``adding a tile creates an external link`` () =
                let addedTile = PathWorld.addTile random tileSet Point.zero withLinks

                addedTile |> Option.isSome |> should equal true

                let expectedTileMap =
                    [
                        Point.zero, Tile.deadEndE
                    ]
                    |> Map.ofList

                addedTile.Value.model.tileMap |> should equal expectedTileMap

                let expectedExternalMap =
                    [
                        Point.create 1 0, set [Direction.West]
                    ]
                    |> Map.ofList

                addedTile.Value.model.external |> should equal expectedExternalMap

                addedTile.Value.activeLinks |> Map.isEmpty |> should equal true

        [<TestFixture>]
        module ``adding a tile next to an active external link`` =
            let bounds = Rect.create 0 0 1 1
            let tileSet = TileSet.create [ Tile.deadEndE, 1u, set [0] ]
            let external = [Point.create 1 0, set [Direction.West]] |> Map.ofList

            let model = PathWorld.create Map.empty external bounds
            let withLinks = PathWorld.getLinks model
            let addedTile = PathWorld.addTile random tileSet Point.zero withLinks
            addedTile |> Option.isSome |> should equal true

            [<Test>]
            let ``links are correct`` () =
                withLinks.activeLinks |> should equal external

            [<Test>]
            let ``removes the active external link`` () =
                addedTile.Value.activeLinks |> Map.isEmpty |> should equal true

        [<TestFixture>]
        module ``adding a tile creates a new active link and removes previous one`` =
            let bounds = Rect.create 0 0 3 1
            let tileSet = TileSet.create [ Tile.straightEW, 1u, set [0] ]
            let tileMap = [ Point.zero, Tile.deadEndE] |> Map.ofList
            let atPoint = Point.create 1 0

            let model = PathWorld.create tileMap Map.empty bounds
            let withLinks = PathWorld.getLinks model
            let addedTile = PathWorld.addTile random tileSet atPoint withLinks

            [<Test>]
            let ``links are correct`` () =
                let expected =
                    [
                        atPoint, set [Direction.East]
                    ]
                    |> Map.ofList

                addedTile |> Option.isSome |> should equal true
                addedTile.Value.activeLinks |> should equal expected

        [<TestFixture>]
        module ``adding a tile that removes some active links on a tile`` =
            let bounds = Rect.create 0 0 3 1
            let centerPoint = Point.create 1 0
            let tileSet = TileSet.create [ Tile.deadEndE, 1u, set [0] ]
            let tileMap = [ centerPoint, Tile.straightEW ] |> Map.ofList

            let model = PathWorld.create tileMap Map.empty bounds
            let withLinks = PathWorld.getLinks model
            let addedTile = PathWorld.addTile random tileSet Point.zero withLinks

            [<Test>]
            let ``links are correct`` () =
                let expected =
                    [
                        centerPoint, set [Direction.East]
                    ]
                    |> Map.ofList

                addedTile |> Option.isSome |> should equal true
                addedTile.Value.activeLinks |> should equal expected

    [<TestFixture>]
    module ``fill`` =

        let tileSet =
            TileSet.full
                {
                    deadEnd = 1u
                    straight = 30u
                    L = 12u
                    T = 3u
                    cross = 1u
                }

        [<Test>]
        let ``terminates`` () =
            let random = System.Random()

            let bounds = Rect.create 0 0 18 12
            let tileMap = Map.empty
            let external = [ Point.create -1 3, set [Direction.East] ] |> Map.ofList

            let model = PathWorld.create tileMap external bounds

            let filled = PathWorld.fill random tileSet model

            let dbg = PathWorld.debugPrint filled

            filled.bounds |> should equal bounds
