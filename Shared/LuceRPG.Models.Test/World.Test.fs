namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module World =

    [<TestFixture>]
    module addObject =

        [<TestFixture>]
        module ``for a rect world`` =
            let bounds = Rect.create 0 0 10 8
            let emptyWorld = World.empty [bounds]

            [<TestFixture>]
            module ``with a wall`` =
                let topLeft = Point.create 1 0
                let wall = WorldObject.create  1 WorldObject.Type.Wall topLeft

                let world =
                   emptyWorld
                    |> World.addObject wall

                [<TestFixture>]
                module ``adding an unblocked wall`` =
                    let newTopLeft = Point.create 5 4
                    let newWall = WorldObject.create 2 WorldObject.Type.Wall newTopLeft

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
                    let newWall = WorldObject.create wall.id wall.t (Point.create 3 1)
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
                                Point.create 3 1
                                Point.create 3 2
                                Point.create 4 1
                                Point.create 4 2
                            ]
                            |> Set.ofList

                        blockedPoints |> should be (equivalent expected)

            [<Test>]
            let ``adding a wall out of bounds fails`` () =
                let topLeft = Point.create 100 100
                let wall = WorldObject.create 1 WorldObject.Type.Wall topLeft

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject 1
                |> should equal false

            [<Test>]
            let ``contains correct points`` () =
                let points =
                    [
                        (Point.create 0 0, true)
                        (Point.create -1 0, false)
                        (Point.create 20 20, false)
                    ]

                points
                |> List.map (fun (p,e) ->
                    World.pointInBounds p emptyWorld |> should equal e
                )
                |> ignore

        [<TestFixture>]
        module ``for a world made of two rects`` =
            let bounds =
                [
                    Rect.create 0 0 5 2
                    Rect.create 3 2 4 4
                ]
            let emptyWorld = World.empty bounds

            [<Test>]
            let ``wall can be placed in first rect`` () =
                let topLeft = Point.create 1 0
                let wall = WorldObject.create 1 WorldObject.Type.Wall topLeft

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal true

            [<Test>]
            let ``wall can be placed in second rect`` () =
                let topLeft = Point.create 4 3
                let wall = WorldObject.create 1 WorldObject.Type.Wall topLeft

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal true

            [<Test>]
            let ``wall can be placed on boundary between rects`` () =
                let topLeft = Point.create 3 1
                let wall = WorldObject.create 1 WorldObject.Type.Wall topLeft

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal true

            [<Test>]
            let ``wall cannot be placed partially in world`` () =
                // top right square is out of bounds
                let topLeft = Point.create 4 1
                let wall = WorldObject.create 1 WorldObject.Type.Wall topLeft

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal false
