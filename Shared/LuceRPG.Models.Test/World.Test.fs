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
            let spawnPoint = Point.create 0 4
            let emptyWorld = World.empty "test-world" [bounds] spawnPoint

            [<TestFixture>]
            module ``with a wall`` =
                let btmLeft = Point.create 1 1
                let wall = WorldObject.create WorldObject.Type.Wall btmLeft |> TestUtil.withId

                let world =
                   emptyWorld
                    |> World.addObject wall

                [<Test>]
                let ``world is created correctly`` () =
                    world.objects
                    |> Map.containsKey wall.id
                    |> should equal true

                [<TestFixture>]
                module ``adding an unblocked wall`` =
                    let newBtmLeft = Point.create 5 4
                    let newWall = WorldObject.create WorldObject.Type.Wall newBtmLeft |> TestUtil.withId

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
                                Point.create 0 4
                                Point.create 0 5
                                Point.create 1 4
                                Point.create 1 5

                                // wall
                                Point.create 3 4
                                Point.create 3 5
                                Point.create 4 4
                                Point.create 4 5
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
                let btmLeft = Point.create 100 100
                let wall = WorldObject.create WorldObject.Type.Wall btmLeft |> TestUtil.withId

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal false

            [<Test>]
            let ``contains correct points`` () =
                let points =
                    [
                        (Point.create 0 0, true)        // btm left
                        (Point.create 0 8, false)       // top left
                        (Point.create 8 0, true)        // btm right
                        (Point.create 8 8, false)       // top right
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
                let player = TestUtil.makePlayer bounds.btmLeft

                let withPlayer = World.addObject player emptyWorld

                World.containsObject player.id withPlayer |> should equal true

            [<Test>]
            let ``wall cannot be added on top of the spawn point`` () =
                let wall =
                    WorldObject.create WorldObject.Type.Wall spawnPoint
                    |> WithId.create

                let withWall = World.addObject wall emptyWorld

                World.containsObject wall.id withWall |> should equal false

        [<TestFixture>]
        module ``for a world made of two rects`` =
            let bounds =
                [
                    Rect.create 0 0 5 2
                    Rect.create 3 -4 4 4
                ]
            let emptyWorld = World.empty "two-rects" bounds (Point.create 0 2)

            [<Test>]
            let ``wall can be placed in first rect`` () =
                let btmLeft = Point.create 2 0
                let wall = WorldObject.create WorldObject.Type.Wall btmLeft |> TestUtil.withId

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal true

            [<Test>]
            let ``wall can be placed in second rect`` () =
                let btmLeft = Point.create 4 -3
                let wall = WorldObject.create WorldObject.Type.Wall btmLeft |> TestUtil.withId

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal true

            [<Test>]
            let ``wall can be placed on boundary between rects`` () =
                let btmLeft = Point.create 3 -1
                let wall = WorldObject.create WorldObject.Type.Wall btmLeft |> TestUtil.withId

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal true

            [<Test>]
            let ``wall cannot be placed partially in world`` () =
                // top right point is out of bounds
                let btmLeft = Point.create 4 -1
                let wall = WorldObject.create WorldObject.Type.Wall btmLeft |> TestUtil.withId

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal false
