namespace LuceRPG.Models

open NUnit.Framework
open FsUnit
open IntentionProcessing

[<TestFixture>]
module IntentionProcessing =

    [<TestFixture>]
    module ``processWorld`` =

        let clientId = "client"
        let worldId = "world-id"
        let username = "some-user"
        let bound = Rect.create 0 0 10 10
        let spawnPoint = Point.create 1 1

        [<TestFixture>]
        module ``for a world with a single wall and player`` =
            let player = TestUtil.makePlayerWithName (Point.create 1 1) username
            let wall = WorldObject.create WorldObject.Type.Wall (Point.create 3 1) |> TestUtil.withId
            let objectClientMap = [player.id, clientId] |> Map.ofList
            let tObjectClientMap = objectClientMap |> Option.Some
            let usernameClientMap = [username, clientId] |> Map.ofList
            let clientWorldMap = [clientId, worldId] |> Map.ofList
            let wocm = [worldId, objectClientMap] |> Map.ofList
            let serverSideData =
                ServerSideData.create wocm usernameClientMap clientWorldMap worldId
            let now = 120L

            let world =
                World.createWithObjs "test-world" [bound] spawnPoint [player; wall]

            let idWorld = world |> WithId.useId worldId
            let worldMap = [idWorld.id, idWorld] |> Map.ofList

            let processFn =
                IntentionProcessing.processWorld
                    now
                    tObjectClientMap
                    Map.empty
                    idWorld

            let makeIntention =
                    Intention.makePayload clientId
                    >> TestUtil.withId
                    >> WithTimestamp.create now
                    >> IndexedIntention.create worldId

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
                        let newPlayer = result.world.value.objects |> Map.tryFind player.id
                        newPlayer.IsSome |> should equal true

                        newPlayer.Value.value.btmLeft |> should equal (Point.create 1 2)

                    [<Test>]
                    let ``nothing is delayed`` () =
                        result.delayed |> Seq.isEmpty |> should equal true

                    [<Test>]
                    let ``world id is unchanged`` () =
                        result.world.id |> should equal idWorld.id

                [<TestFixture>]
                module ``when another client tries to move the player one square north`` =
                    let intention =
                        Intention.Move (player.id, Direction.North, 1uy)
                        |> Intention.makePayload "other-client"
                        |> TestUtil.withId
                        |> WithTimestamp.create 100L
                        |> IndexedIntention.create worldId

                    let result = processFn intention

                    [<Test>]
                    let ``a moved event is not created`` () =
                        let worldEvents = result.events |> List.ofSeq
                        worldEvents.Length |> should equal 0

                    [<Test>]
                    let ``the player object is not moved`` () =
                        let newPlayer = result.world.value.objects |> Map.tryFind player.id
                        newPlayer.IsSome |> should equal true

                        newPlayer.Value.value.btmLeft |> should equal (Point.create 1 1)

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
                        let newPlayer = result.world.value.objects |> Map.tryFind player.id
                        newPlayer.IsSome |> should equal true

                        newPlayer.Value
                        |> WithId.value
                        |> WorldObject.btmLeft
                        |> should equal (Point.create 1 1)

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
                        let newPlayer = result.world.value.objects |> Map.tryFind player.id
                        newPlayer.IsSome |> should equal true

                        newPlayer.Value.value.btmLeft |> should equal (Point.create 1 1)

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
                        let newPlayer = result.world.value.objects |> Map.tryFind player.id
                        newPlayer.IsSome |> should equal true

                        newPlayer.Value.value.btmLeft |> should equal (Point.create 1 0)

                    [<Test>]
                    let ``the rest of the move intention is delayed`` () =
                        let delayed = result.delayed |> List.ofSeq

                        let expected =
                            Intention.Move (player.id, Direction.South, 1uy)
                            |> Intention.makePayload clientId
                            |> WithId.useId intention.tsIntention.value.id
                            |> WithTimestamp.create intention.tsIntention.timestamp
                            |> IndexedIntention.useIndex 1 worldId

                        delayed.Length |> should equal 1
                        delayed |> should equal [expected]

                [<TestFixture>]
                module ``incorrect client id`` =
                    let intention =
                        Intention.Move (player.id, Direction.North, 1uy)
                        |> Intention.makePayload "not-the-client"
                        |> WithId.create
                        |> WithTimestamp.create 100L
                        |> IndexedIntention.create worldId

                    [<TestFixture>]
                    module ``with objectClientMap`` =
                        let processResult =
                            IntentionProcessing.processWorld
                                100L
                                tObjectClientMap
                                Map.empty
                                idWorld
                                intention

                        [<Test>]
                        let ``player can't be moved`` () =
                            processResult.events
                            |> Seq.length
                            |> should equal 0

                    [<TestFixture>]
                    module ``when objectClientMap is not provided`` =
                        let processResult =
                            IntentionProcessing.processWorld
                                100L
                                Option.None
                                Map.empty
                                idWorld
                                intention

                        [<Test>]
                        let ``player can be moved`` () =
                            processResult.events
                            |> Seq.length
                            |> should equal 1

                [<TestFixture>]
                module ``when a move with index 1 is processed`` =
                    let intention =
                        Intention.Move (player.id, Direction.North, 1uy)
                        |> Intention.makePayload clientId
                        |> TestUtil.withId
                        |> WithTimestamp.create 100L
                        |> IndexedIntention.useIndex 1 worldId

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
                            IntentionProcessing.processWorld
                                now
                                tObjectClientMap
                                objectBusyMap
                                idWorld
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
                        IntentionProcessing.processWorld
                            now
                            tObjectClientMap
                            objectBusyMap
                            idWorld

                    [<TestFixture>]
                    module ``processing an intention from during busy period`` =
                        let timestamp = now - 30L
                        let intention =
                            Intention.Move (player.id, Direction.North, 1uy)
                            |> Intention.makePayload clientId
                            |> TestUtil.withId
                            |> WithTimestamp.create timestamp
                            |> IndexedIntention.create worldId

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
                            |> IndexedIntention.create worldId

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

            [<Test>]
            let ``join game does nothing`` () =
                let intention =
                    Intention.JoinGame "new-user"
                    |> makeIntention
                let result = processFn intention

                result.world.value.objects
                |> Map.count
                |> should equal (Map.count world.objects)

            [<Test>]
            let ``leave game does nothing`` () =
                let intention = makeIntention Intention.LeaveGame
                let result = processFn intention

                result.world.value.objects
                |> Map.containsKey player.id
                |> should equal true

    [<TestFixture>]
    module ``processGlobal`` =

        [<TestFixture>]
        module ``for an existing client with a player`` =
            let existingUsername = "some-user"
            let existingClient = "client"
            let existingPlayer = TestUtil.makePlayerWithName (Point.create 0 0) existingUsername

            let spawnPoint = Point.create 4 4
            let emptyWorld = World.empty "empty" [Rect.create 0 0 10 10] spawnPoint
            let world1 = WithId.create (emptyWorld |> World.addObject existingPlayer)
            let world2 = WithId.create emptyWorld

            let worldMap = WithId.toMap [world1; world2]
            let objectClientMap = [existingPlayer.id, existingClient] |> Map.ofList
            let usernameClientMap = [existingUsername, existingClient] |> Map.ofList
            let clientWorldMap = [existingClient, world1.id] |> Map.ofList
            let wocm = [world1.id, objectClientMap] |> Map.ofList

            let serverSideData =
                ServerSideData.create
                    wocm
                    usernameClientMap
                    clientWorldMap
                    world1.id

            [<TestFixture>]
            module ``join game for new username`` =
                let newClientId = "new-client"
                let newUsername = "new-username"

                let intention =
                    Intention.JoinGame newUsername
                    |> Intention.makePayload newClientId
                    |> WithId.create
                    |> WithTimestamp.create 100L
                    |> IndexedIntention.create ""

                let processResult =
                    IntentionProcessing.processGlobal
                        serverSideData
                        Map.empty
                        worldMap
                        intention

                [<Test>]
                let ``creates object added event`` () =
                    let events = processResult.events |> Seq.toList

                    events |> List.length |> should equal 1
                    events.Head.resultOf |> should equal intention.tsIntention.value.id
                    events.Head.t |> should be (ofCase <@WorldEvent.ObjectAdded@>)

                [<Test>]
                let ``adds a new object to default world`` () =
                    let world = processResult.worldMap |> Map.find world1.id
                    world.value.objects |> Map.count |> should equal 2

                [<Test>]
                let ``other world is unaffected`` () =
                    let world = processResult.worldMap |> Map.find world2.id
                    world.value.objects |> Map.count |> should equal 0

                [<Test>]
                let ``other world is unchanged`` () =
                    processResult.worldMap
                    |> Map.find world2.id
                    |> should equal world2

                [<Test>]
                let ``adds a new player with event id`` () =
                    let world = processResult.worldMap |> Map.find world1.id

                    let newPlayer =
                        World.objectList world.value
                        |> List.find (
                            fun p -> WorldObject.isPlayer p.value
                                        && p.id <> existingPlayer.id
                        )

                    newPlayer.value.t
                    |> should be (ofCase <@WorldObject.Type.Player@>)

                    newPlayer.value.btmLeft |> should equal spawnPoint

                [<Test>]
                let ``existing player is unaffected`` () =
                    let world = processResult.worldMap |> Map.find world1.id

                    world.value.objects
                        |> Map.containsKey existingPlayer.id
                        |> should equal true

                    world.value.objects
                        |> Map.find existingPlayer.id
                        |> should equal existingPlayer

                [<Test>]
                let ``adds new player to client object map`` () =
                    let world = processResult.worldMap |> Map.find world1.id

                    let newPlayer =
                        World.objectList world.value
                        |> List.find (
                            fun p -> WorldObject.isPlayer p.value
                                        && p.id <> existingPlayer.id
                        )

                    let tOcm =
                        processResult.serverSideData.worldObjectClientMap
                        |> Map.tryFind world1.id

                    tOcm.IsSome |> should equal true

                    let tEntry = tOcm.Value |> Map.tryFind newPlayer.id
                    tEntry.IsSome |> should equal true
                    tEntry.Value |> should equal newClientId

                [<Test>]
                let ``adds new username and client to usernameClientMap`` () =
                    let ucm = processResult.serverSideData.usernameClientMap

                    ucm |> Map.containsKey newUsername |> should equal true
                    ucm |> Map.find newUsername |> should equal newClientId

                [<Test>]
                let ``adds client to the clientWorldMap`` () =
                    let cwm = processResult.serverSideData.clientWorldMap

                    cwm |> Map.containsKey newClientId |> should equal true
                    cwm |> Map.find newClientId |> should equal world1.id

            [<TestFixture>]
            module ``join game for existing username`` =
                let newClientId = "new-client"

                let intention =
                    Intention.JoinGame existingUsername
                    |> Intention.makePayload newClientId
                    |> WithId.create
                    |> WithTimestamp.create 100L
                    |> IndexedIntention.create ""

                let processResult =
                    IntentionProcessing.processGlobal
                        serverSideData
                        Map.empty
                        worldMap
                        intention

                [<Test>]
                let ``creates object added event`` () =
                    let events = processResult.events |> Seq.toList

                    events |> List.length |> should equal 1
                    events.Head.resultOf |> should equal intention.tsIntention.value.id
                    events.Head.t |> should be (ofCase <@WorldEvent.ObjectAdded@>)

                [<Test>]
                let ``delays a leave game intention for old client id`` () =
                    let delayed = processResult.delayed |> Seq.toList

                    delayed |> List.length |> should equal 1
                    delayed.Head.tsIntention.value.value.t |> should be (ofCase <@Intention.LeaveGame@>)
                    delayed.Head.tsIntention.value.value.clientId |> should equal existingClient

            [<TestFixture>]
            module ``leave game`` =
                let objectBusyMap = [existingPlayer.id, 100L] |> Map.ofList

                let intention =
                    Intention.LeaveGame
                    |> Intention.makePayload existingClient
                    |> WithId.create
                    |> WithTimestamp.create 100L
                    |> IndexedIntention.create ""

                let processResult =
                    IntentionProcessing.processGlobal
                        serverSideData
                        objectBusyMap
                        worldMap
                        intention

                [<Test>]
                let ``creates object removed event`` () =
                    let events = processResult.events |> Seq.toList

                    events |> List.length |> should equal 1
                    events.Head.resultOf |> should equal intention.tsIntention.value.id
                    events.Head.t |> should be (ofCase <@WorldEvent.ObjectRemoved@>)

                [<Test>]
                let ``removes player object from world`` () =
                    let world = processResult.worldMap |> Map.find world1.id

                    world.value
                    |> World.containsObject existingPlayer.id
                    |> should equal false

                    world.value.objects
                    |> Map.count
                    |> should equal 0

                [<Test>]
                let ``removes object from objectBusyMap`` () =
                    processResult.objectBusyMap
                    |> Map.containsKey existingPlayer.id
                    |> should equal false

                [<Test>]
                let ``removes entry from serverSideData`` () =
                    let ucm = processResult.serverSideData.usernameClientMap
                    ucm |> Map.containsKey existingUsername |> should equal false

                    let wocm = processResult.serverSideData.worldObjectClientMap
                    let ocm = wocm |> Map.tryFind world1.id
                    ocm.IsSome |> should equal true
                    ocm.Value
                        |> Map.filter (fun oId cId -> cId <> existingClient)
                        |> Map.count
                        |> should equal 0

                    let cwm = processResult.serverSideData.clientWorldMap
                    cwm |> Map.containsKey existingClient |> should equal false

            [<Test>]
            let ``move object does nothing`` () =
                let intention =
                    Intention.Move (existingPlayer.id, Direction.North, 1uy)
                    |> Intention.makePayload existingClient
                    |> WithId.create
                    |> WithTimestamp.create 100L
                    |> IndexedIntention.create world1.id

                let processResult =
                    IntentionProcessing.processGlobal
                        serverSideData
                        Map.empty
                        worldMap
                        intention

                processResult.worldMap
                |> should equal worldMap

        [<TestFixture>]
        module ``for a client that owns multiple objects on different maps`` =
            let clientId = "client-id"
            let username = "user-name"

            let player1 = TestUtil.makePlayer (Point.create 1 3)
            let player2 = TestUtil.makePlayer (Point.create 3 3)

            let objectClientMap =
                [
                    player1.id, clientId
                    player2.id, clientId
                ]
                |> Map.ofList

            let tObjectClientMap = objectClientMap |> Option.Some

            let bounds = [Rect.create 0 0 10 10]
            let spawnPoint = Point.create 0 0

            let world1 = World.createWithObjs "world-1" bounds spawnPoint [player1] |> WithId.create
            let world2 = World.createWithObjs "world-2" bounds spawnPoint [player2] |> WithId.create

            let worldMap = WithId.toMap [world1; world2]

            let wocm =
                let ocm1 = [player1.id, clientId] |> Map.ofList
                let ocm2 = [player2.id, clientId] |> Map.ofList

                [world1.id, ocm1; world2.id, ocm2] |> Map.ofList

            let usernameClientMap = [username, clientId] |> Map.ofList
            let clientWorldMap = [clientId, world1.id] |> Map.ofList

            let serverSideData =
                ServerSideData.create
                    wocm
                    usernameClientMap
                    clientWorldMap
                    world1.id

            [<Test>]
            let ``worlds create correctly`` () =
                world1.value |> World.containsObject player1.id |> should equal true
                world1.value |> World.containsObject player2.id |> should equal false

                world2.value |> World.containsObject player1.id |> should equal false
                world2.value |> World.containsObject player2.id |> should equal true

            [<TestFixture>]
            module ``leave game`` =
                let intention =
                    Intention.LeaveGame
                    |> Intention.makePayload clientId
                    |> WithId.create
                    |> WithTimestamp.create 100L
                    |> IndexedIntention.create ""

                let processResult =
                    IntentionProcessing.processGlobal
                        serverSideData
                        Map.empty
                        worldMap
                        intention

                [<Test>]
                let ``creates object removed events`` () =
                    let events = processResult.events |> Set.ofSeq

                    let expected =
                        [
                            WorldEvent.asResult
                                intention.tsIntention.value.id
                                world1.id
                                0
                                (WorldEvent.ObjectRemoved player1.id)

                            WorldEvent.asResult
                                intention.tsIntention.value.id
                                world2.id
                                0
                                (WorldEvent.ObjectRemoved player2.id)
                        ]
                        |> Set.ofList

                    events |> should equal expected

                [<Test>]
                let ``removes player objects from world`` () =
                    processResult.worldMap
                    |> Map.find world1.id
                    |> fun w -> w.value
                    |> World.containsObject player1.id
                    |> should equal false

                    processResult.worldMap
                    |> Map.find world2.id
                    |> fun w -> w.value
                    |> World.containsObject player2.id
                    |> should equal false

    [<TestFixture>]
    module ``processMany`` =

        [<TestFixture>]
        module ``for client with single object`` =
            let now = 12L

            let existingUsername = "some-user"
            let existingClient = "client"
            let existingPlayer = TestUtil.makePlayerWithName (Point.create 0 0) existingUsername

            let spawnPoint = Point.create 4 4
            let emptyWorld = World.empty "empty" [Rect.create 0 0 10 10] spawnPoint
            let world1 = WithId.create (emptyWorld |> World.addObject existingPlayer)
            let world2 = WithId.create emptyWorld

            let worldMap = WithId.toMap [world1; world2]
            let objectClientMap = [existingPlayer.id, existingClient] |> Map.ofList
            let usernameClientMap = [existingUsername, existingClient] |> Map.ofList
            let clientWorldMap = [existingClient, world1.id] |> Map.ofList
            let wocm = [world1.id, objectClientMap] |> Map.ofList

            let serverSideData =
                ServerSideData.create
                    wocm
                    usernameClientMap
                    clientWorldMap
                    world1.id

            [<TestFixture>]
            module ``for multiple intentions`` =
                let intention1 =
                    Intention.Move (existingPlayer.id, Direction.East, 1uy)
                    |> Intention.makePayload existingClient
                    |> WithId.create
                    |> WithTimestamp.create 9L
                    |> IndexedIntention.create world1.id

                let intention2 =
                    Intention.LeaveGame
                    |> Intention.makePayload existingClient
                    |> WithId.create
                    |> WithTimestamp.create 10L
                    |> IndexedIntention.create ""

                let intention3 =
                    Intention.Move (existingPlayer.id, Direction.North, 1uy)
                    |> Intention.makePayload existingClient
                    |> WithId.create
                    |> WithTimestamp.create 11L
                    |> IndexedIntention.create world1.id

                let processFn =
                    IntentionProcessing.processMany
                        now
                        serverSideData
                        Map.empty
                        worldMap

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
                            WorldEvent.Type.Moved (existingPlayer.id, Direction.East)
                            WorldEvent.Type.ObjectRemoved (existingPlayer.id)
                        ]

                    let eventTypes (r: ProcessManyResult): WorldEvent.Type seq =
                        r.events |> Seq.map (fun e -> e.t)

                    i123 |> eventTypes |> should equal expectedEvents
                    i213 |> eventTypes |> should equal expectedEvents
                    i321 |> eventTypes |> should equal expectedEvents

                [<Test>]
                let ``events have correct world id`` () =
                    let worldIds =
                        i213.events
                        |> Seq.map (fun e -> e.world)

                    worldIds |> should equal [world1.id; world1.id]

            [<TestFixture>]
            module ``move intentions for incorrect world`` =
                let intention =
                    Intention.Move (existingPlayer.id, Direction.South, 1uy)
                    |> Intention.makePayload existingClient
                    |> WithId.create
                    |> WithTimestamp.create 9L
                    |> IndexedIntention.create world2.id

                let processResult =
                    IntentionProcessing.processMany
                        now
                        serverSideData
                        Map.empty
                        worldMap
                        [intention]

                [<Test>]
                let ``does not move player`` () =
                    processResult.worldMap |> should equal worldMap
