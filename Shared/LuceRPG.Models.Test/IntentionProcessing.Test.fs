namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module IntentionProcessing =

    [<TestFixture>]
    module ``for a world with a single wall and player`` =
        let bound = Rect.create 0 10 10 10
        let spawnPoint = Point.create 2 9
        let player = WorldObject.create WorldObject.Type.Player (Point.create 1 3) |> TestUtil.withId
        let wall = WorldObject.create WorldObject.Type.Wall (Point.create 3 3) |> TestUtil.withId

        let world = World.createWithObjs [bound] spawnPoint [player; wall]

        [<Test>]
        let ``world created correctly`` () =
            world |> World.containsObject player.id |> should equal true
            world |> World.containsObject wall.id |> should equal true

        [<TestFixture>]
        module ``move`` =

            [<TestFixture>]
            module ``when the player tries to move one square north`` =
                let intention = Intention.Move (player.id, Direction.North, 1uy) |> TestUtil.withId
                let result = IntentionProcessing.processOne intention world

                [<Test>]
                let ``a moved event is created`` () =
                    let worldEvents = result.events |> List.ofSeq
                    worldEvents.Length |> should equal 1

                    let expected = WorldEvent.Moved (player.id, Direction.North, 1uy)
                    worldEvents.Head.t |> should equal expected

                [<Test>]
                let ``the player object is moved correctly`` () =
                    let newPlayer = result.world.objects |> Map.tryFind player.id
                    newPlayer.IsSome |> should equal true

                    newPlayer.Value |> WorldObject.topLeft |> should equal (Point.create 1 4)

            [<TestFixture>]
            module ``when the player tries to move one square east`` =
                // player should be blocked by the wall in this case
                let intention = Intention.Move (player.id, Direction.East, 1uy) |> TestUtil.withId
                let result = IntentionProcessing.processOne intention world

                [<Test>]
                let ``a moved event is not created`` () =
                    let worldEvents = result.events |> List.ofSeq
                    worldEvents.Length |> should equal 0

                [<Test>]
                let ``the player object is not moved`` () =
                    let newPlayer = result.world.objects |> Map.tryFind player.id
                    newPlayer.IsSome |> should equal true

                    newPlayer.Value |> WorldObject.topLeft |> should equal (Point.create 1 3)

            [<TestFixture>]
            module ``when the player tries to move four squares east`` =
                // player should teleport past the wall in this case
                let intention = Intention.Move (player.id, Direction.East, 4uy) |> TestUtil.withId
                let result = IntentionProcessing.processOne intention world

                [<Test>]
                let ``a moved event is created`` () =
                    let worldEvents = result.events |> List.ofSeq
                    worldEvents.Length |> should equal 1

                    let expected = WorldEvent.Moved (player.id, Direction.East, 4uy)
                    worldEvents.Head.t |> should equal expected

                [<Test>]
                let ``the player object is moved`` () =
                    let newPlayer = result.world.objects |> Map.tryFind player.id
                    newPlayer.IsSome |> should equal true

                    newPlayer.Value |> WorldObject.topLeft |> should equal (Point.create 5 3)

            [<TestFixture>]
            module ``when the player tries to move two squares south `` =
                // player would move out of bounds from this move
                let intention = Intention.Move (player.id, Direction.South, 2uy) |> TestUtil.withId
                let result = IntentionProcessing.processOne intention world

                [<Test>]
                let ``a moved event is not created`` () =
                    let worldEvents = result.events |> List.ofSeq
                    worldEvents.Length |> should equal 0

                [<Test>]
                let ``the player object is not moved`` () =
                    let newPlayer = result.world.objects |> Map.tryFind player.id
                    newPlayer.IsSome |> should equal true

                    newPlayer.Value |> WorldObject.topLeft |> should equal (Point.create 1 3)

        [<TestFixture>]
        module ``join game`` =
            let intention = Intention.JoinGame |> WithId.create
            let processResult = IntentionProcessing.processOne intention world

            [<Test>]
            let ``creates joined game event`` () =
                let events = processResult.events |> Seq.toList

                events |> List.length |> should equal 1
                events.Head.resultOf |> should equal intention.id
                events.Head.t |> should be (ofCase <@WorldEvent.GameJoined@>)

            [<Test>]
            let ``adds a new player with event id`` () =
                let newPlayer =
                    World.objectList processResult.world
                    |> List.find (
                        fun p ->
                            p.id <> player.id
                            && WorldObject.t p = WorldObject.Type.Player
                    )

                newPlayer
                |> WorldObject.t
                |> should be (ofCase <@WorldObject.Type.Player@>)

            [<Test>]
            let ``old player is unaffected`` () =
                let oldPlayer =
                    World.objectList world
                    |> List.find (fun p -> p.id = player.id)

                oldPlayer |> should equal player
