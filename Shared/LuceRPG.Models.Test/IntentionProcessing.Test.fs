namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module IntentionProcessing =

    [<TestFixture>]
    module ``for a world with a single wall and player`` =
        let clientId = "client"
        let bound = Rect.create 0 10 10 10
        let spawnPoint = Point.create 2 9
        let player = WorldObject.create WorldObject.Type.Player (Point.create 1 3) |> TestUtil.withId
        let wall = WorldObject.create WorldObject.Type.Wall (Point.create 3 3) |> TestUtil.withId
        let clientObjectMap = [player.id, clientId] |> Map.ofList

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

                let result = IntentionProcessing.processOne clientObjectMap world intention

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
                    result.objectClientMap |> should equal clientObjectMap

            [<TestFixture>]
            module ``when another client tries to move the player one square north`` =
                let intention =
                    Intention.Move (player.id, Direction.North, 1uy)
                    |> Intention.makePayload "other-client"
                    |> TestUtil.withId

                let result = IntentionProcessing.processOne clientObjectMap world intention

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

                let result = IntentionProcessing.processOne clientObjectMap world intention

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

                let result = IntentionProcessing.processOne clientObjectMap world intention

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

                let result = IntentionProcessing.processOne clientObjectMap world intention

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
