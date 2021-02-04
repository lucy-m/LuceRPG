﻿namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module World =

    [<TestFixture>]
    module addObject =

        [<TestFixture>]
        module ``for a rect world`` =
            let bounds = Rect.create 0 8 10 8
            let emptyWorld = World.empty [bounds] bounds.topLeft

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
                            WorldObject.getPoints newWall.value
                            |> List.map (fun p -> World.pointBlocked p added)
                            |> List.filter id

                        blockedPoints.Length |> should equal 4

                [<TestFixture>]
                module ``adding a new wall to a different point with the same id`` =
                    let newWall =
                        WithId.useId
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
                                // spawn point
                                Point.create 0 8
                                Point.create 1 8
                                Point.create 0 7
                                Point.create 1 7

                                // wall
                                Point.create 3 4
                                Point.create 3 3
                                Point.create 4 4
                                Point.create 4 3
                            ]
                            |> Set.ofList

                        blockedPoints |> should be (equivalent expected)

                [<TestFixture>]
                module ``with interactions`` =
                    let valid = (wall.id, Id.make())
                    let invalid = (Id.make(), Id.make())
                    let interactions = [valid; invalid] |> Map.ofList

                    let withInteractions = World.setInteractions interactions world

                    [<Test>]
                    let ``interaction created for wall`` () =
                        withInteractions.interactions
                        |> Map.containsKey wall.id
                        |> should equal true

                        withInteractions.interactions
                        |> Map.find wall.id
                        |> should equal (snd valid)

                    [<Test>]
                    let ``interaction ignored for non existant object`` () =
                        withInteractions.interactions
                        |> Map.containsKey (fst invalid)
                        |> should equal false

                    [<Test>]
                    let ``removing wall removes interaction `` () =
                        let withoutWall = World.removeObject wall.id withInteractions

                        withoutWall.interactions
                        |> Map.containsKey wall.id
                        |> should equal false

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

            [<Test>]
            let ``player can be added on top of the spawn point`` () =
                let player = TestUtil.makePlayer bounds.topLeft

                let withPlayer = World.addObject player emptyWorld

                World.containsObject player.id withPlayer |> should equal true

            [<Test>]
            let ``wall cannot be added on top of the spawn point`` () =
                let wall =
                    WorldObject.create WorldObject.Type.Wall bounds.topLeft
                    |> WithId.create

                let withWall = World.addObject wall emptyWorld

                World.containsObject wall.id withWall |> should equal false

        [<TestFixture>]
        module ``for a world made of two rects`` =
            let bounds =
                [
                    Rect.create 0 2 5 2
                    Rect.create 3 0 4 4
                ]
            let emptyWorld = World.empty bounds (Point.create 0 2)

            [<Test>]
            let ``wall can be placed in first rect`` () =
                let topLeft = Point.create 2 2
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
