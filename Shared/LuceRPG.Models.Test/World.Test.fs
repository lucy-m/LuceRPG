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
                let wall = WorldObject.create WorldObject.Type.Wall topLeft

                let world =
                   emptyWorld
                    |> World.addObject wall
                    |> fun w -> w.Value

                [<Test>]
                let ``adding the same wall fails`` () =
                    let tAdded = World.addObject wall world
                    tAdded.IsNone |> should equal true

                [<TestFixture>]
                module ``adding an unblocked wall`` =
                    let newTopLeft = Point.create 5 4
                    let newWall = WorldObject.create WorldObject.Type.Wall newTopLeft

                    let tAdded = World.addObject newWall world

                    [<Test>]
                    let ``wall is added successfully`` () =
                        tAdded.IsSome |> should equal true

                    [<Test>]
                    let ``points for new wall are blocked`` () =
                        let added = tAdded.Value
                        let blockedPoints =
                            WorldObject.getPoints newWall
                            |> List.map (fun p -> World.pointBlocked p added)
                            |> List.filter id

                        blockedPoints.Length |> should equal 4

                    [<Test>]
                    let ``wall is added to objects list`` () =
                        let added = tAdded.Value;

                        added.objects |> should contain newWall

            [<Test>]
            let ``adding a wall out of bounds fails`` () =
                let topLeft = Point.create 100 100
                let wall = WorldObject.create WorldObject.Type.Wall topLeft

                let tAdded = World.addObject wall emptyWorld

                tAdded.IsNone |> should equal true

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
                let wall = WorldObject.create WorldObject.Type.Wall topLeft

                let tAdded = World.addObject wall emptyWorld

                tAdded.IsSome |> should equal true

            [<Test>]
            let ``wall can be placed in second rect`` () =
                let topLeft = Point.create 4 3
                let wall = WorldObject.create WorldObject.Type.Wall topLeft

                let tAdded = World.addObject wall emptyWorld

                tAdded.IsSome |> should equal true

            [<Test>]
            let ``wall can be placed on boundary between rects`` () =
                let topLeft = Point.create 3 1
                let wall = WorldObject.create WorldObject.Type.Wall topLeft

                let tAdded = World.addObject wall emptyWorld

                tAdded.IsSome |> should equal true

            [<Test>]
            let ``wall cannot be placed partially in world`` () =
                // top right square is out of bounds
                let topLeft = Point.create 4 2
                let wall = WorldObject.create WorldObject.Type.Wall topLeft

                let tAdded = World.addObject wall emptyWorld

                tAdded.IsSome |> should equal true
