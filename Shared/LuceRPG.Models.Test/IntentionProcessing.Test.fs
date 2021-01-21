﻿namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module IntentionProcessing =

    [<TestFixture>]
    module ``for a world with a single wall and player`` =
        let bound = Rect.create 0 0 10 10
        let player = WorldObject.create 1 WorldObject.Type.Player (Point.create 1 1)
        let wall = WorldObject.create 2 WorldObject.Type.Wall (Point.create 3 1)

        let world = World.createWithObjs [bound] [player; wall]

        [<Test>]
        let ``world created correctly`` () =
            world |> World.containsObject player.id |> should equal true
            world |> World.containsObject wall.id |> should equal true

        [<TestFixture>]
        module ``when the player tries to move one square north`` =
            let intention = Intention.Move (player.id, Direction.North, 1uy)
            let result = IntentionProcessing.processOne intention world

            [<Test>]
            let ``a moved event is created`` () =
                let worldEvents = result.events
                worldEvents.Length |> should equal 1

                let expected = WorldEvent.Moved (player.id, Direction.North, 1uy)
                worldEvents.Head |> should equal expected

            [<Test>]
            let ``the player object is moved correctly`` () =
                let newPlayer = result.world.objects |> Map.tryFind player.id
                newPlayer.IsSome |> should equal true

                newPlayer.Value.topLeft |> should equal (Point.create 1 2)

        [<TestFixture>]
        module ``when the player tries to move one square east`` =
            // player should be blocked by the wall in this case
            let intention = Intention.Move (player.id, Direction.East, 1uy)
            let result = IntentionProcessing.processOne intention world

            [<Test>]
            let ``a moved event is not created`` () =
                let worldEvents = result.events
                worldEvents.Length |> should equal 0

            [<Test>]
            let ``the player object is not moved`` () =
                let newPlayer = result.world.objects |> Map.tryFind player.id
                newPlayer.IsSome |> should equal true

                newPlayer.Value.topLeft |> should equal (Point.create 1 1)

        [<TestFixture>]
        module ``when the player tries to move four squares east`` =
            // player should teleport past the wall in this case
            let intention = Intention.Move (player.id, Direction.East, 4uy)
            let result = IntentionProcessing.processOne intention world

            [<Test>]
            let ``a moved event is created`` () =
                let worldEvents = result.events
                worldEvents.Length |> should equal 1

                let expected = WorldEvent.Moved (player.id, Direction.East, 4uy)
                worldEvents.Head |> should equal expected

            [<Test>]
            let ``the player object is moved`` () =
                let newPlayer = result.world.objects |> Map.tryFind player.id
                newPlayer.IsSome |> should equal true

                newPlayer.Value.topLeft |> should equal (Point.create 5 1)

        [<TestFixture>]
        module ``when the player tries to move two squares south `` =
            // player would move out of bounds from this move
            let intention = Intention.Move (player.id, Direction.South, 2uy)
            let result = IntentionProcessing.processOne intention world

            [<Test>]
            let ``a moved event is not created`` () =
                let worldEvents = result.events
                worldEvents.Length |> should equal 0

            [<Test>]
            let ``the player object is not moved`` () =
                let newPlayer = result.world.objects |> Map.tryFind player.id
                newPlayer.IsSome |> should equal true

                newPlayer.Value.topLeft |> should equal (Point.create 1 1)