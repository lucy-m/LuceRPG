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
            let emptyWorld = World.empty "test-world" [bounds] spawnPoint WorldBackground.GreenGrass

            [<TestFixture>]
            module ``with a wall`` =
                let btmLeft = Point.create 1 1
                let wall = WorldObject.create WorldObject.Type.Wall btmLeft Direction.South |> TestUtil.withId

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
                    let newWall = WorldObject.create WorldObject.Type.Wall newBtmLeft Direction.South |> TestUtil.withId

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
                            |> Seq.map (fun p -> World.pointBlocked p added)
                            |> Seq.filter id

                        blockedPoints |> Seq.length |> should equal 4

                [<TestFixture>]
                module ``adding a new wall to a different point with the same id`` =
                    let newWall =
                        WithId.useId
                            wall.id
                            (WorldObject.create wall.value.t (Point.create 3 4) Direction.South)
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
                let wall = WorldObject.create WorldObject.Type.Wall btmLeft Direction.South |> TestUtil.withId

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
                    WorldObject.create WorldObject.Type.Wall spawnPoint Direction.South
                    |> WithId.create

                let withWall = World.addObject wall emptyWorld

                World.containsObject wall.id withWall |> should equal false

            [<TestFixture>]
            module ``adding a warp`` =
                let toWorldId = "other-world"
                let toPoint = Point.zero
                let warpData = Warp.create (Warp.createTarget toWorldId toPoint) Warp.Appearance.Door
                let btmLeft = Point.create 5 4
                let warp =
                    WorldObject.create (WorldObject.Type.Warp warpData) btmLeft Direction.South
                    |> TestUtil.withId

                let added = World.addObject warp emptyWorld

                [<Test>]
                let ``warp object is added`` () =
                    added.objects |> Map.containsKey warp.id |> should equal true
                    added.objects |> Map.find warp.id |> should equal warp

                [<Test>]
                let ``warps map is correct`` () =
                    let expectedPoints = WorldObject.getPoints warp.value

                    expectedPoints
                    |> Seq.forall (fun p ->
                        added.warps |> Map.containsKey p
                        &&
                            added.warps
                            |> Map.find p
                            |> fun (woId, wd) -> woId = warp.id
                    )
                    |> should equal true

                    added.warps
                    |> Map.find btmLeft
                    |> fun (woId, wd) ->
                        wd.toWorld |> should equal toWorldId
                        wd.toPoint |> should equal toPoint

                [<TestFixture>]
                module ``when the warp overlaps a player`` =
                    let playerPoint = Direction.movePoint Direction.North 1 btmLeft
                    let player = TestUtil.makePlayer playerPoint
                    let withPlayer = World.addObject player added

                    [<Test>]
                    let ``player is added correctly`` () =
                        withPlayer.objects |> Map.containsKey player.id |> should equal true

                    [<Test>]
                    let ``getWarps returns correct warp`` () =
                        let tWarpData = World.getWarp player.id withPlayer
                        tWarpData.IsSome |> should equal true

                        tWarpData.Value.toWorld |> should equal toWorldId
                        tWarpData.Value.toPoint |> should equal toPoint

                [<TestFixture>]
                module ``removing the warp`` =
                    let removed = World.removeObject warp.id added

                    [<Test>]
                    let ``updates the warps map correctly`` () =
                        removed.warps |> Map.isEmpty |> should equal true

            [<TestFixture>]
            module ``adding an inn`` =
                let btmLeft = Point.create 2 0
                let warpData = Warp.createTarget "toWorld" Point.zero |> Option.Some
                let inn =
                    WorldObject.create (WorldObject.Type.Inn warpData) btmLeft Direction.South
                    |> TestUtil.withId

                let added = World.addObject inn emptyWorld

                [<Test>]
                let ``inn is added`` () =
                    added.objects |> Map.containsKey inn.id |> should equal true
                    added.objects |> Map.find inn.id |> should equal inn

                [<Test>]
                let ``warp is added`` () =
                    let warpPos = Point.create 5 1
                    let warpAtPos = added.warps |> Map.tryFind warpPos

                    warpAtPos.IsSome |> should equal true
                    warpAtPos.Value |> snd |> should equal warpData.Value

            [<TestFixture>]
            module ``with paths`` =
                // Paths overlap at point 3,0

                let path1 =
                    WorldObject.Type.Path (Point.create 4 1)
                    |> fun t -> WorldObject.create t Point.zero Direction.South
                    |> WithId.create

                let path2 =
                    WorldObject.Type.Path (Point.create 2 2)
                    |> fun t -> WorldObject.create t (Point.create 3 0) Direction.South
                    |> WithId.create

                let pathWorld = World.addObjects [path1; path2] emptyWorld

                [<Test>]
                let ``paths returns correct value`` () =
                    let expected =
                        [
                            0,0; 1,0; 2,0; 3,0;     // from path 1
                            4,0; 3,1; 4,1;          // from path 2
                        ]
                        |> List.map (fun (x,y) -> Point.create x y)
                        |> Set.ofList

                    let actual = World.paths pathWorld

                    actual |> should equal expected

        [<TestFixture>]
        module ``for a world made of two rects`` =
            let bounds =
                [
                    Rect.create 0 0 5 2
                    Rect.create 3 -4 4 4
                ]
            let emptyWorld = World.empty "two-rects" bounds (Point.create 0 2) WorldBackground.GreenGrass

            [<Test>]
            let ``wall can be placed in first rect`` () =
                let btmLeft = Point.create 2 0
                let wall = WorldObject.create WorldObject.Type.Wall btmLeft Direction.South |> TestUtil.withId

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal true

            [<Test>]
            let ``wall can be placed in second rect`` () =
                let btmLeft = Point.create 4 -3
                let wall = WorldObject.create WorldObject.Type.Wall btmLeft Direction.South |> TestUtil.withId

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal true

            [<Test>]
            let ``wall can be placed on boundary between rects`` () =
                let btmLeft = Point.create 3 -1
                let wall = WorldObject.create WorldObject.Type.Wall btmLeft Direction.South |> TestUtil.withId

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal true

            [<Test>]
            let ``wall cannot be placed partially in world`` () =
                // top right point is out of bounds
                let btmLeft = Point.create 4 -1
                let wall = WorldObject.create WorldObject.Type.Wall btmLeft Direction.South |> TestUtil.withId

                let added = World.addObject wall emptyWorld

                added
                |> World.containsObject wall.id
                |> should equal false
