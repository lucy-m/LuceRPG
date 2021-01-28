﻿namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module IntentionProcessing =
    let clientId = "client"
    let bound = Rect.create 0 10 10 10
    let spawnPoint = Point.create 2 9

    [<TestFixture>]
    module ``for a world with a single wall and player`` =
        let player = WorldObject.create WorldObject.Type.Player (Point.create 1 3) |> TestUtil.withId
        let wall = WorldObject.create WorldObject.Type.Wall (Point.create 3 3) |> TestUtil.withId
        let objectClientMap = [player.id, clientId] |> Map.ofList

        let world = World.createWithObjs [bound] spawnPoint [player; wall]

        [<Test>]
        let ``world created correctly`` () =
            world |> World.containsObject player.id |> should equal true
            world |> World.containsObject wall.id |> should equal true

        [<TestFixture>]
        module ``move`` =

            [<TestFixture>]
            module ``when the player tries to move one square north`` =
                let intention =
                    Intention.Move (player.id, Direction.North, 1uy)
                    |> Intention.makePayload clientId
                    |> TestUtil.withId

                let result = IntentionProcessing.processOne objectClientMap world intention

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

                [<Test>]
                let ``client object map is unchanged`` () =
                    result.objectClientMap |> should equal objectClientMap

            [<TestFixture>]
            module ``when another client tries to move the player one square north`` =
                let intention =
                    Intention.Move (player.id, Direction.North, 1uy)
                    |> Intention.makePayload "other-client"
                    |> TestUtil.withId

                let result = IntentionProcessing.processOne objectClientMap world intention

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
            module ``when the player tries to move one square east`` =
                // player should be blocked by the wall in this case
                let intention =
                    Intention.Move (player.id, Direction.East, 1uy)
                    |> Intention.makePayload clientId
                    |> TestUtil.withId

                let result = IntentionProcessing.processOne objectClientMap world intention

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
                let intention =
                    Intention.Move (player.id, Direction.East, 4uy)
                    |> Intention.makePayload clientId
                    |> TestUtil.withId

                let result = IntentionProcessing.processOne objectClientMap world intention

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
                let intention =
                    Intention.Move (player.id, Direction.South, 2uy)
                    |> Intention.makePayload clientId
                    |> TestUtil.withId

                let result = IntentionProcessing.processOne objectClientMap world intention

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
            let newClientId = "new-client"

            let intention =
                Intention.JoinGame
                |> Intention.makePayload newClientId
                |> WithId.create
            let processResult = IntentionProcessing.processOne Map.empty world intention

            [<Test>]
            let ``creates object added event`` () =
                let events = processResult.events |> Seq.toList

                events |> List.length |> should equal 1
                events.Head.resultOf |> should equal intention.id
                events.Head.t |> should be (ofCase <@WorldEvent.ObjectAdded@>)

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

            [<Test>]
            let ``adds new player to client object map`` () =
                let newPlayer =
                    World.objectList processResult.world
                    |> List.find (
                        fun p ->
                            p.id <> player.id
                            && WorldObject.t p = WorldObject.Type.Player
                    )

                let tEntry =
                    processResult.objectClientMap
                    |> Map.tryFind newPlayer.id

                tEntry.IsSome |> should equal true

                tEntry.Value |> should equal newClientId

        [<TestFixture>]
        module ``leave game`` =
            let intention =
                Intention.LeaveGame
                |> Intention.makePayload clientId
                |> WithId.create

            let processResult = IntentionProcessing.processOne objectClientMap world intention

            [<Test>]
            let ``creates object removed event`` () =
                let events = processResult.events |> Seq.toList

                events |> List.length |> should equal 1
                events.Head.resultOf |> should equal intention.id
                events.Head.t |> should be (ofCase <@WorldEvent.ObjectRemoved@>)

            [<Test>]
            let ``removes player object from world`` () =
                processResult.world
                |> World.containsObject player.id
                |> should equal false

        [<TestFixture>]
        module ``for multiple intentions`` =
            let intention1 =
                Intention.Move (player.id, Direction.East, 1uy)
                |> Intention.makePayload clientId
                |> WithId.create
                |> WithTimestamp.create 9L

            let intention2 =
                Intention.LeaveGame
                |> Intention.makePayload clientId
                |> WithId.create
                |> WithTimestamp.create 10L

            let intention3 =
                Intention.Move (player.id, Direction.North, 1uy)
                |> Intention.makePayload clientId
                |> WithId.create
                |> WithTimestamp.create 11L

            let doProcess = IntentionProcessing.processMany objectClientMap world

            let i123 = doProcess [intention1; intention2; intention3]
            let i213 = doProcess [intention2; intention1; intention3]
            let i321 = doProcess [intention3; intention2; intention1]

            [<Test>]
            let ``processes correctly regardless of input order`` () =
                i123 |> should equal i213
                i213 |> should equal i321

            [<Test>]
            let ``returns events in correct order`` () =
                let expectedEvents =
                    [
                        WorldEvent.Type.Moved (player.id, Direction.East, 1uy)
                        WorldEvent.Type.ObjectRemoved (player.id)
                    ]

                i123.events |> should equal expectedEvents
                i213.events |> should equal expectedEvents
                i321.events |> should equal expectedEvents

    [<TestFixture>]
    module ``for a client that owns multiple objects`` =
        let player1 = WorldObject.create WorldObject.Type.Player (Point.create 1 3) |> TestUtil.withId
        let player2 = WorldObject.create WorldObject.Type.Player (Point.create 3 3) |> TestUtil.withId

        let objectClientMap =
            [
                player1.id, clientId
                player2.id, clientId
            ]
            |> Map.ofList

        let world = World.createWithObjs [bound] spawnPoint [player1; player2]

        [<Test>]
        let ``world create correctly`` () =
            world |> World.containsObject player1.id |> should equal true
            world |> World.containsObject player2.id |> should equal true

        [<TestFixture>]
        module ``leave game`` =
            let intention =
                Intention.LeaveGame
                |> Intention.makePayload clientId
                |> WithId.create

            let processResult = IntentionProcessing.processOne objectClientMap world intention

            [<Test>]
            let ``creates object removed events`` () =
                let events = processResult.events |> Seq.toList

                events |> List.length |> should equal 2
                events.Head.resultOf |> should equal intention.id
                events.Head.t |> should be (ofCase <@WorldEvent.ObjectRemoved@>)
                events.Tail.Head.resultOf |> should equal intention.id
                events.Tail.Head.t |> should be (ofCase <@WorldEvent.ObjectRemoved@>)

            [<Test>]
            let ``removes player objects from world`` () =
                processResult.world
                |> World.containsObject player1.id
                |> should equal false

                processResult.world
                |> World.containsObject player2.id
                |> should equal false

