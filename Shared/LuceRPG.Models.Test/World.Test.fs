namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module World =

    [<TestFixture>]
    module addObject =

        [<TestFixture>]
        module ``for a rect world`` =
            let bounds = Rect.create 0 8 10 8
            let emptyWorld = World.empty [bounds]

            [<TestFixture>]
            module ``with a wall`` =
                let topLeft = Point.create 1 3
                let wall = WorldObject.create WorldObject.Type.Wall topLeft |> TestUtil.withId

                let world =
                   emptyWorld
                    |> World.addObject wall

                [<TestFixture>]
                module ``adding an unblocked wall`` =
                    let newTopLeft = Point.create 5 4
                    let newWall = WorldObject.create WorldObject.Type.Wall newTopLeft |> TestUtil.withId

                    let added = World.addObject newWall world

                    [<Test>]
                    let ``wall is added successfully`` () =
                        added
                        |> World.containsObject newWall.id
                        |> should equal true

                    [<Test>]
                    let ``points for new wall are blocked`` () =
                        let blockedPoints =
                            WorldObject.getPoints newWall
                            |> List.map (fun p -> World.pointBlocked p added)
                            |> List.filter id

                        blockedPoints.Length |> should equal 4

                [<TestFixture>]
                module ``adding a new wall to a different point with the same id`` =
                    let newWall =
                        WithId.create
                            wall.id
                            (WorldObject.create (WorldObject.t wall) (Point.create 3 4))
                    let newWorld = World.addObject newWall world

                    [<Test>]
                    let ``wall is added successfully`` () =
                        World.objectList newWorld
                        |> should contain newWall

                    [<Test>]
                    let ``removes the old wall`` () =
                        World.objectList newWorld
                        |> should not' (contain wall)

                    [<Test>]
                    let ``updates the blocking map correctly`` () =
                        let blockedPoints =
                            newWorld.blocked
                            |> Map.toList
                            |> List.map fst
                            |> Set.ofList

                        let expected =
                            [
                                Point.create 3 4
                                Point.create 3 3
                                Point.create 4 4
                                Point.create 4 3
                            ]
                            |> Set.ofList

                        blockedPoints |> should be (equivalent expected)

            [<Test>]
            let ``adding a wall out of bounds fails`` () =
                let topLeft = Point.create 100 100
                let wall = WorldObject.create WorldObject.Type.Wall topLeft |> TestUtil.withId

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal false

            [<Test>]
            let ``contains correct points`` () =
                let points =
                    [
                        (Point.create 0 8, true)        // top left
                        (Point.create 9 8, true)        // top right
                        (Point.create 0 0, false)        // bottom left
                        (Point.create 9 0, false)        // bottom right
                        (Point.create -1 0, false)
                        (Point.create 20 20, false)
                    ]

                points
                |> List.map (fun (p,e) ->
                    World.pointInBounds p emptyWorld |> should equal e
                )
                |> ignore

            [<TestFixture>]
            module ``adding a spawner`` =
                let spawner =
                    WorldObject.create WorldObject.Type.PlayerSpawner (Point.create  2 6)
                    |> TestUtil.withId
                let withSpawner = World.addObject spawner emptyWorld

                [<Test>]
                let ``adds spawner correctly`` () =
                    withSpawner.playerSpawner.IsSome |> should equal true

                [<Test>]
                let ``updates blocking map correctly`` () =
                    let expected =
                        [
                            Point.create 2 6
                            Point.create 2 5
                            Point.create 3 6
                            Point.create 3 5
                        ]
                        |> Set.ofList

                    let blocked =
                        withSpawner.blocked
                        |> Map.toList
                        |> List.map fst
                        |> Set.ofList

                    blocked |> should be (equivalent expected)

                [<TestFixture>]
                module ``adding a new spawner`` =
                    let newSpawner =
                        WorldObject.create WorldObject.Type.PlayerSpawner (Point.create 5 6)
                        |> TestUtil.withId
                    let withNewSpawner = World.addObject newSpawner withSpawner

                    [<Test>]
                    let ``updates player spawner`` () =
                        withNewSpawner.playerSpawner.IsSome |> should equal true
                        withNewSpawner.playerSpawner.Value |> should equal newSpawner

                    [<Test>]
                    let ``removes old spawner`` () =
                        withNewSpawner
                        |> World.containsObject spawner.id
                        |> should equal false

        [<TestFixture>]
        module ``for a world made of two rects`` =
            let bounds =
                [
                    Rect.create 0 2 5 2
                    Rect.create 3 0 4 4
                ]
            let emptyWorld = World.empty bounds

            [<Test>]
            let ``wall can be placed in first rect`` () =
                let topLeft = Point.create 1 2
                let wall = WorldObject.create WorldObject.Type.Wall topLeft |> TestUtil.withId

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal true

            [<Test>]
            let ``wall can be placed in second rect`` () =
                let topLeft = Point.create 4 -1
                let wall = WorldObject.create WorldObject.Type.Wall topLeft |> TestUtil.withId

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal true

            [<Test>]
            let ``wall can be placed on boundary between rects`` () =
                let topLeft = Point.create 4 0
                let wall = WorldObject.create WorldObject.Type.Wall topLeft |> TestUtil.withId

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal true

            [<Test>]
            let ``wall cannot be placed partially in world`` () =
                // top right point is out of bounds
                let topLeft = Point.create 4 1
                let wall = WorldObject.create WorldObject.Type.Wall topLeft |> TestUtil.withId

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal false
