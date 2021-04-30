namespace LuceRPG.Server.Core.WorldGenerator

open NUnit.Framework
open LuceRPG.Models
open FsUnit
open FsCheck

[<TestFixture>]
module PathWorld =

    [<TestFixture>]
    module ``getLinks`` =
        [<Test>]
        let ``correct for 2x2 world with no externals`` () =
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
                    Direction.movePoint Direction.East 1 btmRight, Direction.East
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
            let external = [ externalPoint, Direction.West ] |> Map.ofList

            let model = PathWorld.create Map.empty external bounds

            let withLinks = PathWorld.getLinks model

            let expected = [ externalPoint, set[Direction.East]] |> Map.ofList

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
                        Point.create 1 0, Direction.East
                    ]
                    |> Map.ofList

                addedTile.Value.model.externalMap |> should equal expectedExternalMap

                addedTile.Value.activeLinks |> Map.isEmpty |> should equal true

        [<TestFixture>]
        module ``adding a tile next to an active external link`` =
            let bounds = Rect.create 0 0 1 1
            let tileSet = TileSet.create [ Tile.deadEndE, 1u, set [0] ]
            let external = [Point.create 1 0, Direction.East] |> Map.ofList

            let model = PathWorld.create Map.empty external bounds
            let withLinks = PathWorld.getLinks model
            let addedTile = PathWorld.addTile random tileSet Point.zero withLinks
            addedTile |> Option.isSome |> should equal true

            [<Test>]
            let ``links are correct`` () =
                let expected = [Point.create 1 0, set [Direction.West]] |> Map.ofList
                withLinks.activeLinks |> should equal expected

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
        let ``does not change world bounds`` () =
            let checkFn (seed: int): bool =
                let random = System.Random(seed)

                let bounds = Rect.create 0 0 10 10
                let tileMap = Map.empty
                let external = [ Point.create -1 3, Direction.East ] |> Map.ofList

                let model = PathWorld.create tileMap external bounds

                let filled = PathWorld.fill tileSet random model

                filled.bounds = bounds

            Check.QuickThrowOnFailure checkFn

        [<Test>]
        let ``same seed produces same result`` () =
            let checkFn (seed: int): bool =
                let r1 = System.Random(seed)
                let r2 = System.Random(seed)

                let bounds = Rect.create 0 0 10 10
                let tileMap = Map.empty
                let external = [ Point.create -1 3, Direction.East ] |> Map.ofList

                let model = PathWorld.create tileMap external bounds

                let filled1 = PathWorld.fill tileSet r1 model
                let filled2 = PathWorld.fill tileSet r2 model

                filled1 = filled2

            Check.QuickThrowOnFailure checkFn

        [<Test>]
        let ``for a partially filled map, will include the given tiles`` () =
            let checkFn (seed: int): bool =
                let random = System.Random(seed)

                let bounds = Rect.create 0 0 10 10
                let tileMap =
                    [
                        0, 0, Tile.LNE
                        0, 1, Tile.straightEW
                        4, 4, Tile.cross
                    ]
                    |> Point.toPointMap
                let external = Map.empty

                let model = PathWorld.create tileMap external bounds

                let filled = PathWorld.fill tileSet random model

                let originalTiles =
                    tileMap
                    |> Map.toSeq
                    |> Set.ofSeq

                filled.tileMap
                |> Map.toSeq
                |> Set.ofSeq
                |> Set.isSubset originalTiles

            Check.QuickThrowOnFailure checkFn

        [<Test>]
        let ``empty model produces empty result`` () =
            let random = System.Random()

            let bounds = Rect.create 0 0 10 10
            let tileMap = Map.empty
            let external = Map.empty

            let model = PathWorld.create tileMap external bounds
            let filled = PathWorld.fill tileSet random model

            filled.tileMap |> Map.isEmpty |> should equal true

    [<TestFixture>]
    module ``getDirMostTiles`` =
        let bounds = Rect.create 0 0 5 5

        let northMost = 2, 4  // Filled to edge
        let westMost = set [ 0, 2; 0, 3 ] // Filled to edge
        let southMost = 2, 1  // Not filled to edge
        let eastMost = 4, 2   // Filled to edge

        let tileMap =
            [
                northMost; southMost; eastMost
            ]
            |> Seq.append westMost
            |> Seq.map (fun (x, y) -> Point.create x y, Tile.deadEnd)
            |> Map.ofSeq

        let model = PathWorld.create tileMap Map.empty bounds

        [<Test>]
        let ``north correct`` () =
            let isFilled, tiles = PathWorld.getDirMostTiles Direction.North model

            isFilled |> should equal true
            tiles |> List.length |> should equal 1

            let tileCoOrds = tiles.Head |> fst

            (tileCoOrds.x, tileCoOrds.y) |> should equal northMost

        [<Test>]
        let ``west correct`` () =
            let isFilled, tiles = PathWorld.getDirMostTiles Direction.West model

            isFilled |> should equal true
            tiles |> List.length |> should equal 2

            let actual = tiles |> List.map (fun (p, t) -> p.x, p.y) |> Set.ofList
            actual |> should equal westMost

        [<Test>]
        let ``south correct`` () =
            let isFilled, tiles = PathWorld.getDirMostTiles Direction.South model

            isFilled |> should equal false
            tiles |> List.length |> should equal 1

            let tileCoOrds = tiles.Head |> fst

            (tileCoOrds.x, tileCoOrds.y) |> should equal southMost

        [<Test>]
        let ``east correct`` () =
            let isFilled, tiles = PathWorld.getDirMostTiles Direction.East model

            isFilled |> should equal true
            tiles |> List.length |> should equal 1

            let tileCoOrds = tiles.Head |> fst

            (tileCoOrds.x, tileCoOrds.y) |> should equal eastMost

    [<TestFixture>]
    module ``fillInDirection`` =
        let random = System.Random()

        [<TestFixture>]
        module ``given partially filled world`` =
            let bounds = Rect.create 0 0 10 10

            let tileMap =       // Create an L shape in SW corner
                [
                    0, 1, Tile.deadEndE
                    1, 1, Tile.LSW
                    1, 0, Tile.deadEndN
                ]
                |> Point.toPointMap

            let initial = PathWorld.create tileMap Map.empty bounds

            let dbg = PathWorld.debugPrint initial

            [<Test>]
            let ``fill north correct`` () =
                let northFilled =
                    PathWorld.fillInDirection
                        TileSet.fullUniform
                        Direction.North
                        random
                        initial

                let filledDbg = PathWorld.debugPrint northFilled

                let isFilled, _ = PathWorld.getDirMostTiles Direction.North northFilled

                isFilled |> should equal true

            [<Test>]
            let ``fill east correct`` () =
                let eastFilled =
                    PathWorld.fillInDirection
                        TileSet.fullUniform
                        Direction.East
                        random
                        initial

                let filledDbg = PathWorld.debugPrint eastFilled

                let isFilled, _ = PathWorld.getDirMostTiles Direction.East eastFilled

                isFilled |> should equal true

            [<Test>]
            let ``fill west does nothing`` () =
                let westFilled =
                    PathWorld.fillInDirection
                        TileSet.fullUniform
                        Direction.West
                        random
                        initial

                westFilled |> should equal initial

            [<Test>]
            let ``fill south does nothing`` () =
                let southFilled =
                    PathWorld.fillInDirection
                        TileSet.fullUniform
                        Direction.South
                        random
                        initial

                southFilled |> should equal initial

            [<Test>]
            let ``given same seed will produce same map`` () =
                let checkFn (seed: int): bool =
                    let r1 = System.Random(seed)
                    let r2 = System.Random(seed)

                    let f1 =
                        PathWorld.fillInDirection
                            TileSet.fullUniform
                            Direction.North
                            r1
                            initial

                    let f2 =
                        PathWorld.fillInDirection
                            TileSet.fullUniform
                            Direction.North
                            r2
                            initial

                    f1 = f2

                Check.QuickThrowOnFailure checkFn

    [<TestFixture>]
    module ``generateFilled`` =

        [<TestFixture>]
        module ``for full tile set`` =
            let tileSet = TileSet.fullUniform

            [<Test>]
            let ``creates external links on all edges`` () =
                let checkFn (): bool =
                    let random = System.Random()
                    let bounds = Rect.create 0 0 10 10

                    let generated =
                        PathWorld.generateFilled
                            bounds
                            tileSet
                            random

                    let tilePoints =
                        generated.tileMap
                        |> Map.toSeq
                        |> Seq.map fst

                    let minX, maxX =
                        tilePoints
                        |> Seq.map (fun p -> p.x)
                        |> fun s -> Seq.min s, Seq.max s

                    let minY, maxY =
                        tilePoints
                        |> Seq.map (fun p -> p.y)
                        |> fun s -> Seq.min s, Seq.max s

                    let dbg = PathWorld.debugPrint generated

                    minX = Rect.leftBound bounds
                    && maxX = (Rect.rightBound bounds - 1)
                    && minY = Rect.bottomBound bounds
                    && maxY = (Rect.topBound bounds - 1)

                Check.QuickThrowOnFailure checkFn
