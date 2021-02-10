﻿namespace LuceRPG.Models

open NUnit.Framework
open FsUnit
open IntentionProcessing

[<TestFixture>]
module IntentionProcessing =
    let clientId = "client"
    let bound = Rect.create 0 0 10 10
    let spawnPoint = Point.create 1 1

    [<TestFixture>]
    module ``for a world with a single wall and player`` =
        let player = TestUtil.makePlayer (Point.create 1 1)
        let wall = WorldObject.create WorldObject.Type.Wall (Point.create 3 1) |> TestUtil.withId
        let objectClientMap = [player.id, clientId] |> Map.ofList |> Option.Some
        let now = 120L

        let world = World.createWithObjs [bound] spawnPoint [player; wall]

        let processFn = IntentionProcessing.processOne now objectClientMap Map.empty world
        let makeIntention =
                Intention.makePayload clientId
                >> TestUtil.withId
                >> WithTimestamp.create now
                >> IndexedIntention.create

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
                    |> makeIntention

                let result = processFn intention

                [<Test>]
                let ``a moved event is created`` () =
                    let worldEvents = result.events |> List.ofSeq
                    worldEvents.Length |> should equal 1

                    let expected = WorldEvent.Moved (player.id, Direction.North)
                    worldEvents.Head.t |> should equal expected

                [<Test>]
                let ``the player object is moved correctly`` () =
                    let newPlayer = result.world.objects |> Map.tryFind player.id
                    newPlayer.IsSome |> should equal true

                    newPlayer.Value |> WorldObject.btmLeft |> should equal (Point.create 1 2)

                [<Test>]
                let ``client object map is unchanged`` () =
                    result.objectClientMap |> should equal objectClientMap

                [<Test>]
                let ``nothing is delayed`` () =
                    result.delayed |> Seq.isEmpty |> should equal true

            [<TestFixture>]
            module ``when another client tries to move the player one square north`` =
                let intention =
                    Intention.Move (player.id, Direction.North, 1uy)
                    |> Intention.makePayload "other-client"
                    |> TestUtil.withId
                    |> WithTimestamp.create 100L
                    |> IndexedIntention.create

                let result = processFn intention

                [<Test>]
                let ``a moved event is not created`` () =
                    let worldEvents = result.events |> List.ofSeq
                    worldEvents.Length |> should equal 0

                [<Test>]
                let ``the player object is not moved`` () =
                    let newPlayer = result.world.objects |> Map.tryFind player.id
                    newPlayer.IsSome |> should equal true

                    newPlayer.Value |> WorldObject.btmLeft |> should equal (Point.create 1 1)

            [<TestFixture>]
            module ``when the player tries to move one square east`` =
                // player should be blocked by the wall in this case
                let intention =
                    Intention.Move (player.id, Direction.East, 1uy)
                    |> makeIntention

                let result = processFn intention

                [<Test>]
                let ``a moved event is not created`` () =
                    let worldEvents = result.events |> List.ofSeq
                    worldEvents.Length |> should equal 0

                [<Test>]
                let ``the player object is not moved`` () =
                    let newPlayer = result.world.objects |> Map.tryFind player.id
                    newPlayer.IsSome |> should equal true

                    newPlayer.Value |> WorldObject.btmLeft |> should equal (Point.create 1 1)

            [<TestFixture>]
            module ``when the player tries to move four squares east`` =
                // player is blocked by the wall
                let intention =
                    Intention.Move (player.id, Direction.East, 4uy)
                    |> makeIntention

                let result = processFn intention

                [<Test>]
                let ``a moved event is not created`` () =
                    let worldEvents = result.events |> List.ofSeq
                    worldEvents.Length |> should equal 0

                [<Test>]
                let ``the player object is not moved`` () =
                    let newPlayer = result.world.objects |> Map.tryFind player.id
                    newPlayer.IsSome |> should equal true

                    newPlayer.Value |> WorldObject.btmLeft |> should equal (Point.create 1 1)

            [<TestFixture>]
            module ``when the player tries to move two squares south `` =
                // Player would move out of bounds from this move
                //   but intention is partially applied to move one
                //   square south
                let intention =
                    Intention.Move (player.id, Direction.South, 2uy)
                    |> makeIntention

                let result = processFn intention

                [<Test>]
                let ``a moved event is created`` () =
                    let worldEvents = result.events |> List.ofSeq
                    worldEvents.Length |> should equal 1

                    let expected = WorldEvent.Moved (player.id, Direction.South)
                    worldEvents.Head.t |> should equal expected

                [<Test>]
                let ``the player object is moved correctly`` () =
                    let newPlayer = result.world.objects |> Map.tryFind player.id
                    newPlayer.IsSome |> should equal true

                    newPlayer.Value |> WorldObject.btmLeft |> should equal (Point.create 1 0)

                [<Test>]
                let ``the rest of the move intention is delayed`` () =
                    let delayed = result.delayed |> List.ofSeq

                    let expected =
                        Intention.Move (player.id, Direction.South, 1uy)
                        |> Intention.makePayload clientId
                        |> WithId.useId intention.tsIntention.value.id
                        |> WithTimestamp.create intention.tsIntention.timestamp
                        |> IndexedIntention.useIndex 1

                    delayed.Length |> should equal 1
                    delayed |> should equal [expected]

            [<TestFixture>]
            module ``when a move with index 1 is processed`` =
                let intention =
                    Intention.Move (player.id, Direction.North, 1uy)
                    |> Intention.makePayload clientId
                    |> TestUtil.withId
                    |> WithTimestamp.create 100L
                    |> IndexedIntention.useIndex 1

                let result = processFn intention

                [<Test>]
                let ``a moved event is created`` () =
                    let worldEvents = result.events |> List.ofSeq
                    worldEvents.Length |> should equal 1

                    let expected = WorldEvent.Moved (player.id, Direction.North)
                    worldEvents.Head.t |> should equal expected

                [<Test>]
                let ``moved event has correct index`` () =
                    let movedEvent =
                        result.events
                        |> Seq.head

                    movedEvent.index |> should equal intention.index

            [<TestFixture>]
            module ``when the player is busy`` =
                let objectBusyMap = Map.ofList [(player.id, now + 20L)]

                [<TestFixture>]
                module ``processing an intention now`` =
                    let intention =
                        Intention.Move (player.id, Direction.North, 1uy)
                        |> makeIntention

                    let result =
                        IntentionProcessing.processOne
                            now
                            objectClientMap
                            objectBusyMap
                            world
                            intention

                    [<Test>]
                    let ``intention is delayed`` () =
                        result.delayed |> should equal [intention]

            [<TestFixture>]
            module ``when the player was busy`` =
                let busyUntil = now - 20L
                let objectBusyMap = Map.ofList [(player.id, busyUntil)]
                let travelTime = WorldObject.travelTime player.value

                let processFn =
                    IntentionProcessing.processOne
                        now
                        objectClientMap
                        objectBusyMap
                        world

                [<TestFixture>]
                module ``processing an intention from during busy period`` =
                    let timestamp = now - 30L
                    let intention =
                        Intention.Move (player.id, Direction.North, 1uy)
                        |> Intention.makePayload clientId
                        |> TestUtil.withId
                        |> WithTimestamp.create timestamp
                        |> IndexedIntention.create

                    let result = processFn intention

                    [<Test>]
                    let ``player is busy starting from the end of the busy period`` () =
                        let expectedEnd = busyUntil + travelTime

                        result.objectBusyMap
                        |> Map.find player.id
                        |> should equal expectedEnd

                [<TestFixture>]
                module ``processing an intention from after busy period`` =
                    let timestamp = now - 10L
                    let intention =
                        Intention.Move (player.id, Direction.North, 1uy)
                        |> Intention.makePayload clientId
                        |> TestUtil.withId
                        |> WithTimestamp.create timestamp
                        |> IndexedIntention.create

                    let result = processFn intention

                    [<Test>]
                    let ``player is busy starting from the intention timestamp`` () =
                        let expectedEnd = timestamp + travelTime

                        result.objectBusyMap
                        |> Map.find player.id
                        |> should equal expectedEnd

            [<TestFixture>]
            module ``when the wall tries to move`` =
                let intention =
                    Intention.Move (wall.id, Direction.North, 1uy)
                    |> makeIntention

                let result = processFn intention

                [<Test>]
                let ``nothing happens`` () =
                    result.events |> Seq.isEmpty |> should equal true

        [<TestFixture>]
        module ``join game`` =
            let newClientId = "new-client"

            let intention =
                Intention.JoinGame "username"
                |> Intention.makePayload newClientId
                |> WithId.create
                |> WithTimestamp.create 100L
                |> IndexedIntention.create

            let processResult =
                IntentionProcessing.processOne
                    now
                    (Map.empty |> Option.Some)
                    Map.empty
                    world
                    intention

            [<Test>]
            let ``creates object added event`` () =
                let events = processResult.events |> Seq.toList

                events |> List.length |> should equal 1
                events.Head.resultOf |> should equal intention.tsIntention.value.id
                events.Head.t |> should be (ofCase <@WorldEvent.ObjectAdded@>)

            [<Test>]
            let ``adds a new player with event id`` () =
                let newPlayer =
                    World.objectList processResult.world
                    |> List.find (
                        fun p ->
                            p.id <> player.id
                            && WorldObject.isPlayer p
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
                            && WorldObject.isPlayer p
                    )

                let tEntry =
                    processResult.objectClientMap.Value
                    |> Map.tryFind newPlayer.id

                tEntry.IsSome |> should equal true

                tEntry.Value |> should equal newClientId

        [<TestFixture>]
        module ``leave game`` =
            let intention =
                Intention.LeaveGame
                |> Intention.makePayload clientId
                |> WithId.create
                |> WithTimestamp.create 100L
                |> IndexedIntention.create

            let objectBusyMap = [player.id, now] |> Map.ofList

            let processResult =
                IntentionProcessing.processOne
                    now
                    objectClientMap
                    objectBusyMap
                    world
                    intention

            [<Test>]
            let ``creates object removed event`` () =
                let events = processResult.events |> Seq.toList

                events |> List.length |> should equal 1
                events.Head.resultOf |> should equal intention.tsIntention.value.id
                events.Head.t |> should be (ofCase <@WorldEvent.ObjectRemoved@>)

            [<Test>]
            let ``removes player object from world`` () =
                processResult.world
                |> World.containsObject player.id
                |> should equal false

            [<Test>]
            let ``removes object from objectBusyMap`` () =
                processResult.objectBusyMap
                |> Map.containsKey player.id
                |> should equal false

        [<TestFixture>]
        module ``for multiple intentions`` =
            let intention1 =
                Intention.Move (player.id, Direction.South, 1uy)
                |> Intention.makePayload clientId
                |> WithId.create
                |> WithTimestamp.create 9L
                |> IndexedIntention.create

            let intention2 =
                Intention.LeaveGame
                |> Intention.makePayload clientId
                |> WithId.create
                |> WithTimestamp.create 10L
                |> IndexedIntention.create

            let intention3 =
                Intention.Move (player.id, Direction.North, 1uy)
                |> Intention.makePayload clientId
                |> WithId.create
                |> WithTimestamp.create 11L
                |> IndexedIntention.create

            let processFn = IntentionProcessing.processMany now objectClientMap Map.empty world

            let i123 = processFn [intention1; intention2; intention3]
            let i213 = processFn [intention2; intention1; intention3]
            let i321 = processFn [intention3; intention2; intention1]

            [<Test>]
            let ``processes correctly regardless of input order`` () =
                i123 |> should equal i213
                i213 |> should equal i321

            [<Test>]
            let ``returns events in correct order`` () =
                let expectedEvents =
                    [
                        WorldEvent.Type.Moved (player.id, Direction.South)
                        WorldEvent.Type.ObjectRemoved (player.id)
                    ]

                let eventTypes (r: ProcessResult): WorldEvent.Type seq =
                    r.events |> Seq.map (fun e -> e.t)

                i123 |> eventTypes |> should equal expectedEvents
                i213 |> eventTypes |> should equal expectedEvents
                i321 |> eventTypes |> should equal expectedEvents

    [<TestFixture>]
    module ``for a client that owns multiple objects`` =
        let player1 = TestUtil.makePlayer (Point.create 1 3)
        let player2 = TestUtil.makePlayer (Point.create 3 3)

        let objectClientMap =
            [
                player1.id, clientId
                player2.id, clientId
            ]
            |> Map.ofList
            |> Option.Some

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
                |> WithTimestamp.create 100L
                |> IndexedIntention.create

            let processResult =
                IntentionProcessing.processOne 100L objectClientMap Map.empty world intention

            [<Test>]
            let ``creates object removed events`` () =
                let events = processResult.events |> Seq.toList

                events |> List.length |> should equal 2
                events.Head.resultOf |> should equal intention.tsIntention.value.id
                events.Head.t |> should be (ofCase <@WorldEvent.ObjectRemoved@>)
                events.Tail.Head.resultOf |> should equal intention.tsIntention.value.id
                events.Tail.Head.t |> should be (ofCase <@WorldEvent.ObjectRemoved@>)

            [<Test>]
            let ``removes player objects from world`` () =
                processResult.world
                |> World.containsObject player1.id
                |> should equal false

                processResult.world
                |> World.containsObject player2.id
                |> should equal false

        [<TestFixture>]
        module ``when objectClientMap is not provided`` =
            let intentions =
                [
                    Intention.Move (player1.id, Direction.North, 1uy)
                    Intention.Move (player2.id, Direction.North, 1uy)
                ]
                |> List.map (
                    Intention.makePayload "not-the-client"
                    >> WithId.create
                    >> WithTimestamp.create 100L
                    >> IndexedIntention.create
                )

            let processResult =
                IntentionProcessing.processMany
                    100L
                    Option.None
                    Map.empty
                    world
                    intentions

            [<Test>]
            let ``all objects can be moved`` () =
                processResult.events
                |> Seq.length
                |> should equal 2
