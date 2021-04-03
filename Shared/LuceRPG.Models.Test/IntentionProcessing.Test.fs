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
        let serverId = "server" |> Option.Some
        let bound = Rect.create 0 0 10 10
        let spawnPoint = Point.create 1 1

        [<TestFixture>]
        module ``for a world with a single wall and player`` =
            let player = TestUtil.makePlayerWithName (Point.create 1 1) username
            let wall = WorldObject.create WorldObject.Type.Wall (Point.create 3 1) Direction.South |> TestUtil.withId
            let objectClientMap = [player.id, clientId] |> Map.ofList
            let tObjectClientMap = objectClientMap |> Option.Some
            let now = 120L

            let world =
                World.createWithObjs "test-world" [bound] spawnPoint WorldBackground.GreenGrass [player; wall]

            let idWorld = world |> WithId.useId worldId

            let processFn =
                IntentionProcessing.processWorld
                    now
                    tObjectClientMap
                    serverId
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
                        newPlayer.Value.value.facing |> should equal Direction.North

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
                        newPlayer.Value.value.facing |> should equal Direction.South

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

                        newPlayer.Value.value.btmLeft |> should equal (Point.create 1 1)
                        newPlayer.Value.value.facing |> should equal Direction.South

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
                                serverId
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
                module ``server client id`` =
                    let intention =
                        Intention.Move (player.id, Direction.North, 1uy)
                        |> Intention.makePayload serverId.Value
                        |> WithId.create
                        |> WithTimestamp.create 100L
                        |> IndexedIntention.create worldId

                    [<TestFixture>]
                    module ``with objectClientMap`` =
                        let processResult =
                            IntentionProcessing.processWorld
                                100L
                                tObjectClientMap
                                serverId
                                Map.empty
                                idWorld
                                intention

                        [<Test>]
                        let ``player can be moved`` () =
                            processResult.events
                            |> Seq.length
                            |> should equal 1

                    [<TestFixture>]
                    module ``when objectClientMap is not provided`` =
                        let processResult =
                            IntentionProcessing.processWorld
                                100L
                                Option.None
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
                                serverId
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
                            serverId
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
                    let ``logs`` () =
                        result.log.IsSome |> should equal true

            [<TestFixture>]
            module ``turn towards`` =

                [<Test>]
                let ``turn towards north turns player`` () =
                    let dir = Direction.North
                    player.value.facing |> should not' (equal dir)

                    let intention =
                        Intention.TurnTowards (player.id, dir)
                        |> makeIntention

                    let result = processFn intention

                    result.events |> Seq.length |> should equal 1
                    let expected = WorldEvent.TurnedTowards (player.id, dir)
                    result.events |> Seq.head |> fun e -> e.t |> should equal expected

                    let newPlayer = result.world.value.objects |> Map.tryFind player.id
                    newPlayer.IsSome |> should equal true
                    newPlayer.Value.value.btmLeft |> should equal (player.value.btmLeft)
                    newPlayer.Value.value.facing |> should equal dir

                [<Test>]
                let ``turn unknown object is ignored`` () =
                    let intention =
                        Intention.TurnTowards ("unknown", Direction.North)
                        |> makeIntention

                    let result = processFn intention

                    result.events |> Seq.length |> should equal 0

                    let newPlayer = result.world.value.objects |> Map.tryFind player.id
                    newPlayer.IsSome |> should equal true
                    newPlayer.Value.value.btmLeft |> should equal (player.value.btmLeft)
                    newPlayer.Value.value.facing |> should equal (player.value.facing)

                [<Test>]
                let ``incorrect client id is ignored`` () =
                    let intention =
                        Intention.TurnTowards (player.id, Direction.North)
                        |> Intention.makePayload "other-client"
                        |> TestUtil.withId
                        |> WithTimestamp.create 100L
                        |> IndexedIntention.create worldId

                    let result = processFn intention

                    result.events |> Seq.length |> should equal 0

                    let newPlayer = result.world.value.objects |> Map.tryFind player.id
                    newPlayer.IsSome |> should equal true
                    newPlayer.Value.value.btmLeft |> should equal (player.value.btmLeft)
                    newPlayer.Value.value.facing |> should equal (player.value.facing)

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

            [<Test>]
            let ``join world does nothing`` () =
                let newPlayer = TestUtil.makePlayer (Point.create 3 3)
                let intention = makeIntention (Intention.JoinWorld newPlayer)
                let result = processFn intention

                result.world.value.objects
                |> Map.containsKey newPlayer.id
                |> should equal false

        [<TestFixture>]
        module ``for a world with a player and a warp pad`` =
            let player = TestUtil.makePlayer (Point.create 1 1)
            let toWorld = "to-world-id"
            let toPoint = Point.zero
            let warpData = Warp.create (Warp.createTarget toWorld toPoint) Warp.Appearance.Door
            let warp =
                WorldObject.create
                    (WorldObject.Type.Warp warpData)
                    (Point.create 3 1)
                    Direction.South
                |> TestUtil.withId

            let now = 120L

            let world =
                World.createWithObjs "test-world" [bound] spawnPoint WorldBackground.GreenGrass [player; warp]

            let idWorld = world |> WithId.useId worldId

            [<Test>]
            let ``move east delays warp intention`` () =
                let intention =
                    Intention.Move (player.id, Direction.East, 5uy)
                    |> Intention.makePayload clientId
                    |> TestUtil.withId
                    |> WithTimestamp.create now
                    |> IndexedIntention.create worldId

                let processResult =
                    IntentionProcessing.processWorld
                        now
                        Option.None
                        Option.None
                        Map.empty
                        idWorld
                        intention

                let delayed = processResult.delayed |> List.ofSeq
                delayed.Length |> should equal 1

                let warpIntentions =
                    delayed
                    |> List.choose (fun i ->
                        match i.tsIntention.value.value.t with
                        | Intention.Type.Warp (wId, point, objId) ->
                            Option.Some (wId, point, objId, i.worldId)
                        | _ -> Option.None
                    )

                warpIntentions.Length |> should equal 1

                let (toWorldId, point, objId, fromWorldId) = warpIntentions.Head
                toWorldId |> should equal toWorld
                point |> should equal toPoint
                objId |> should equal player.id
                fromWorldId |> should equal idWorld.id

    [<TestFixture>]
    module ``processGlobal`` =
        let serverId = "server"

        [<TestFixture>]
        module ``for an existing client with a player`` =
            let existingUsername = "some-user"
            let existingClient = "client"
            let existingPlayer = TestUtil.makePlayerWithName (Point.create 0 0) existingUsername

            let spawnPoint = Point.create 4 4
            let emptyWorld = World.empty "empty" [Rect.create 0 0 10 10] spawnPoint WorldBackground.GreenGrass
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
                    serverId

            [<TestFixture>]
            module ``join game for new username`` =
                let newClientId = "new-client"
                let newUsername = "new-username"
                let intentionId = "intention"

                let intention =
                    Intention.JoinGame newUsername
                    |> Intention.makePayload newClientId
                    |> WithId.useId intentionId
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

                    events |> List.length |> should equal 2
                    events.Head.resultOf |> should equal intention.tsIntention.value.id
                    events.Head.t |> should be (ofCase <@WorldEvent.ObjectAdded@>)

                    let expectedJoinedWorld =
                        WorldEvent.JoinedWorld newClientId
                        |> WorldEvent.asResult intentionId world1.id 1

                    events.Tail.Head |> should equal expectedJoinedWorld

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

                [<Test>]
                let ``server id is unchanged`` () =
                    processResult.serverSideData.serverId |> should equal serverId

            [<TestFixture>]
            module ``join game for existing username`` =
                let newClientId = "new-client"
                let intentionId = "intention"

                let intention =
                    Intention.JoinGame existingUsername
                    |> Intention.makePayload newClientId
                    |> WithId.useId intentionId
                    |> WithTimestamp.create 100L
                    |> IndexedIntention.create ""

                let processResult =
                    IntentionProcessing.processGlobal
                        serverSideData
                        Map.empty
                        worldMap
                        intention

                [<Test>]
                let ``creates object added event and joined world event`` () =
                    let events = processResult.events |> Seq.toList

                    events |> List.length |> should equal 2
                    events.Head.resultOf |> should equal intention.tsIntention.value.id
                    events.Head.t |> should be (ofCase <@WorldEvent.ObjectAdded@>)

                    let expectedJoinedWorld =
                        WorldEvent.JoinedWorld newClientId
                        |> WorldEvent.asResult intentionId world1.id 1

                    events.Tail.Head |> should equal expectedJoinedWorld

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

            let world1 =
                World.createWithObjs
                    "world-1" bounds spawnPoint WorldBackground.GreenGrass [player1]
                |> WithId.create

            let world2 =
                World.createWithObjs
                    "world-2" bounds spawnPoint WorldBackground.GreenGrass [player2]
                |> WithId.create

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
                    serverId

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

                [<Test>]
                let ``removes entry from clientWorldMap`` () =
                    processResult.serverSideData.clientWorldMap
                    |> Map.containsKey clientId
                    |> should equal false

                [<Test>]
                let ``server id is unchanged`` () =
                    processResult.serverSideData.serverId |> should equal serverId

            [<TestFixture>]
            module ``leave world`` =
                [<TestFixture>]
                module ``from first world`` =
                    let intention =
                        Intention.LeaveWorld
                        |> Intention.makePayload clientId
                        |> WithId.create
                        |> WithTimestamp.create 100L
                        |> IndexedIntention.create world1.id

                    let processResult =
                        IntentionProcessing.processGlobal
                            serverSideData
                            Map.empty
                            worldMap
                            intention

                    [<Test>]
                    let ``creates events to remove first world object`` () =
                        let events = processResult.events |> Set.ofSeq

                        let expected =
                            [
                                WorldEvent.asResult
                                    intention.tsIntention.value.id
                                    world1.id
                                    0
                                    (WorldEvent.ObjectRemoved player1.id)
                            ]
                            |> Set.ofList

                        events |> should equal expected

                    [<Test>]
                    let ``removes only first world player`` () =
                        processResult.worldMap
                        |> Map.find world1.id
                        |> fun w -> w.value
                        |> World.containsObject player1.id
                        |> should equal false

                        processResult.worldMap
                        |> Map.find world2.id
                        |> fun w -> w.value
                        |> World.containsObject player2.id
                        |> should equal true

                    [<Test>]
                    let ``removes entry from clientWorldMap`` () =
                        processResult.serverSideData.clientWorldMap
                        |> Map.containsKey clientId
                        |> should equal false

                    [<Test>]
                    let ``server id is unchanged`` () =
                        processResult.serverSideData.serverId |> should equal serverId

                [<TestFixture>]
                module ``from second world`` =
                    let intention =
                        Intention.LeaveWorld
                        |> Intention.makePayload clientId
                        |> WithId.create
                        |> WithTimestamp.create 100L
                        |> IndexedIntention.create world2.id

                    let processResult =
                        IntentionProcessing.processGlobal
                            serverSideData
                            Map.empty
                            worldMap
                            intention

                    [<Test>]
                    let ``creates events to remove second world object`` () =
                        let events = processResult.events |> Set.ofSeq

                        let expected =
                            [
                                WorldEvent.asResult
                                    intention.tsIntention.value.id
                                    world2.id
                                    0
                                    (WorldEvent.ObjectRemoved player2.id)
                            ]
                            |> Set.ofList

                        events |> should equal expected

                    [<Test>]
                    let ``removes only second world player`` () =
                        processResult.worldMap
                        |> Map.find world1.id
                        |> fun w -> w.value
                        |> World.containsObject player1.id
                        |> should equal true

                        processResult.worldMap
                        |> Map.find world2.id
                        |> fun w -> w.value
                        |> World.containsObject player2.id
                        |> should equal false

                    [<Test>]
                    let ``clientWorldMap is unchanged`` () =
                        processResult.serverSideData.clientWorldMap
                        |> Map.containsKey clientId
                        |> should equal true

                        processResult.serverSideData.clientWorldMap
                        |> should equal serverSideData.clientWorldMap

                    [<Test>]
                    let ``server id is unchanged`` () =
                        processResult.serverSideData.serverId |> should equal serverId

            [<TestFixture>]
            module ``warp`` =
                [<TestFixture>]
                module ``from world1 to world2 valid point`` =
                    let toPoint = Point.create 7 1

                    let intention =
                        Intention.Warp (world2.id, toPoint, player1.id)
                        |> Intention.makePayload clientId
                        |> WithId.create
                        |> WithTimestamp.create 100L
                        |> IndexedIntention.create world1.id

                    let processResult =
                        IntentionProcessing.processGlobal
                            serverSideData
                            Map.empty
                            worldMap
                            intention

                    [<Test>]
                    let ``delays joinWorld and leaveWorld intentions`` () =
                        let delayed = processResult.delayed |> Seq.toList

                        delayed.Length |> should equal 2

                        let joinWorlds =
                            delayed
                            |> List.choose (fun i ->
                                match i.tsIntention.value.value.t with
                                | Intention.JoinWorld obj ->
                                    Option.Some (i.index, i.worldId, obj)
                                | _ -> Option.None
                            )

                        joinWorlds.Length |> should equal 1

                        let (index, wId, obj) = joinWorlds.Head
                        index |> should equal 1
                        wId |> should equal world2.id
                        obj.id |> should equal player1.id
                        obj.value.btmLeft |> should equal toPoint

                        let leaveWorlds =
                            delayed
                            |> List.choose (fun i ->
                                match i.tsIntention.value.value.t with
                                | Intention.LeaveWorld ->
                                    Option.Some (i.index, i.worldId)
                                | _ -> Option.None
                            )

                        leaveWorlds.Length |> should equal 1
                        let (index, wId) = leaveWorlds.Head
                        index |> should equal 1
                        wId |> should equal world1.id

                [<TestFixture>]
                module ``from world1 to invalid world`` =
                    let toPoint = Point.create 100 100
                    let toWorld = "not-a-world"

                    let intention =
                        Intention.Warp (toWorld, toPoint, player1.id)
                        |> Intention.makePayload clientId
                        |> WithId.create
                        |> WithTimestamp.create 100L
                        |> IndexedIntention.create world1.id

                    let processResult =
                        IntentionProcessing.processGlobal
                            serverSideData
                            Map.empty
                            worldMap
                            intention

                    [<Test>]
                    let ``does nothing`` () =
                        processResult.delayed |> Seq.isEmpty |> should equal true

                    [<Test>]
                    let ``logs`` () =
                        processResult.log.IsSome |> should equal true

                [<TestFixture>]
                module ``for invalid object`` =
                    let toPoint = Point.create 100 100
                    let objectId = "not-an-object"

                    let intention =
                        Intention.Warp (world2.id, toPoint, objectId)
                        |> Intention.makePayload clientId
                        |> WithId.create
                        |> WithTimestamp.create 100L
                        |> IndexedIntention.create world1.id

                    let processResult =
                        IntentionProcessing.processGlobal
                            serverSideData
                            Map.empty
                            worldMap
                            intention

                    [<Test>]
                    let ``does nothing`` () =
                        processResult.delayed |> Seq.isEmpty |> should equal true

                    [<Test>]
                    let ``logs`` () =
                        processResult.log.IsSome |> should equal true

    [<TestFixture>]
    module ``processMany`` =
        let serverId = "server"

        [<TestFixture>]
        module ``for client with single object`` =
            let now = 12L

            let existingUsername = "some-user"
            let existingClient = "client"
            let existingPlayer = TestUtil.makePlayerWithName (Point.create 0 0) existingUsername

            let spawnPoint = Point.create 4 4
            let emptyWorld = World.empty "empty" [Rect.create 0 0 10 10] spawnPoint WorldBackground.GreenGrass
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
                    serverId

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

            [<TestFixture>]
            module ``joinWorld`` =

                [<TestFixture>]
                module ``for valid location`` =
                    let obj = TestUtil.makePlayer (Point.create 2 2)

                    let intention =
                        Intention.JoinWorld obj
                        |> Intention.makePayload existingClient
                        |> WithId.create
                        |> WithTimestamp.create now
                        |> IndexedIntention.create world2.id

                    let processResult =
                        IntentionProcessing.processMany
                            now
                            serverSideData
                            Map.empty
                            worldMap
                            [intention]

                    [<Test>]
                    let ``objectClientMap is correct`` () =
                        let wocm = processResult.serverSideData.worldObjectClientMap

                        // Existing player should be unchanged
                        let ocm1 = [existingPlayer.id, existingClient] |> Map.ofList
                        wocm |> Map.containsKey world1.id |> should equal true
                        wocm |> Map.find world1.id |> should equal ocm1

                        // New player should be added
                        let ocm2 = [obj.id, existingClient] |> Map.ofList
                        wocm |> Map.containsKey world2.id |> should equal true
                        wocm |> Map.find world2.id |> should equal ocm2

                    [<Test>]
                    let ``clientWorldMap is updated`` () =
                        let expected = [existingClient, world2.id] |> Map.ofList
                        processResult.serverSideData.clientWorldMap |> should equal expected

                    [<Test>]
                    let ``player is added to the location`` () =
                        processResult.worldMap |> Map.containsKey world1.id |> should equal true
                        processResult.worldMap |> Map.containsKey world2.id |> should equal true

                        let resultWorld1 = processResult.worldMap |> Map.find world1.id
                        resultWorld1.value.objects |> Map.containsKey obj.id |> should equal false

                        let resultWorld2 = processResult.worldMap |> Map.find world2.id
                        resultWorld2.value.objects |> Map.containsKey obj.id |> should equal true

                        let resultObj = resultWorld2.value.objects |> Map.find obj.id
                        resultObj.value.btmLeft |> should equal obj.value.btmLeft

                    [<Test>]
                    let ``creates correct events`` () =
                        let events = processResult.events |> Seq.toList

                        events.Length |> should equal 2
                        events.Head.t |> should be (ofCase <@WorldEvent.Type.ObjectAdded@>)

                        let secondEvent = WorldEvent.Type.JoinedWorld existingClient
                        events.Tail.Head.t |> should equal secondEvent

                [<TestFixture>]
                module ``for invalid location`` =
                    let obj = TestUtil.makePlayer (Point.create 100 100)

                    let intention =
                        Intention.JoinWorld obj
                        |> Intention.makePayload existingClient
                        |> WithId.create
                        |> WithTimestamp.create now
                        |> IndexedIntention.create world2.id

                    let processResult =
                        IntentionProcessing.processMany
                            now
                            serverSideData
                            Map.empty
                            worldMap
                            [intention]

                    [<Test>]
                    let ``objectClientMap is correct`` () =
                        let wocm = processResult.serverSideData.worldObjectClientMap

                        // New player should be added
                        let ocm2 = [obj.id, existingClient] |> Map.ofList
                        wocm |> Map.containsKey world2.id |> should equal true
                        wocm |> Map.find world2.id |> should equal ocm2

                    [<Test>]
                    let ``clientWorldMap is updated`` () =
                        let expected = [existingClient, world2.id] |> Map.ofList
                        processResult.serverSideData.clientWorldMap |> should equal expected

                    [<Test>]
                    let ``player is added to the spawn point`` () =
                        processResult.worldMap |> Map.containsKey world2.id |> should equal true

                        let resultWorld2 = processResult.worldMap |> Map.find world2.id
                        resultWorld2.value.objects |> Map.containsKey obj.id |> should equal true

                        let resultObj = resultWorld2.value.objects |> Map.find obj.id
                        resultObj.value.btmLeft |> should equal spawnPoint

        [<TestFixture>]
        module ``multiple join intentions`` =
            let now = 12L

            let user1 = "user1"
            let user2 = "user2"
            let client1 = "client1"
            let client2 = "client2"

            let defaultWorld =
                World.empty "empty" [Rect.create 0 0 10 10] Point.zero WorldBackground.GreenGrass
                |> WithId.create

            let worldMap = WithId.toMap [defaultWorld]

            let serverSideData = ServerSideData.empty defaultWorld.id

            let intentions =
                [
                    Intention.JoinGame user1 |> Intention.makePayload client1
                    Intention.JoinGame user2 |> Intention.makePayload client2
                ]
                |> List.map (
                    WithId.create
                    >> WithTimestamp.create now
                    >> IndexedIntention.create ""
                )

            let processResult =
                IntentionProcessing.processMany
                    now
                    serverSideData
                    Map.empty
                    worldMap
                    intentions

            [<Test>]
            let ``adds two players to the world`` () =
                let world = processResult.worldMap |> Map.find defaultWorld.id

                world.value.objects |> Map.count |> should equal 2

            [<Test>]
            let ``worldObjectClientMap correct`` () =
                let wocm = processResult.serverSideData.worldObjectClientMap
                wocm |> Map.containsKey defaultWorld.id |> should equal true
                wocm |> Map.count |> should equal 1

                let ocm = wocm |> Map.find defaultWorld.id
                ocm |> Map.count |> should equal 2
                ocm |> Map.exists (fun pId cId -> cId = client1) |> should equal true
                ocm |> Map.exists (fun pId cId -> cId = client2) |> should equal true

