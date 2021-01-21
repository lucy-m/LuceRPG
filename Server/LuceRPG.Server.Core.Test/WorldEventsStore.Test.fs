namespace LuceRPG.Server.Core

open NUnit.Framework
open FsUnit
open LuceRPG.Models
open System

[<TestFixture>]
module WorldEventsStore =

    [<TestFixture>]
    module ``store with two events`` =
        let firstEvent: WorldEventsStore.StoredEvent =
            {
                timestamp = new DateTime(1000L)
                worldEvent = WorldEvent.Moved (1, Direction.North, 1uy)
            }

        let secondEvent: WorldEventsStore.StoredEvent =
            {
                timestamp = new DateTime(1200L)
                worldEvent = WorldEvent.Moved (1, Direction.East, 1uy)
            }

        let events = [firstEvent; secondEvent]
        let world = World.empty []

        let store: WorldEventsStore =
            {
                recentEvents = events
                world = world
            }

        [<Test>]
        let ``getting before both events returns both events`` () =
            let dt = new DateTime(800L)
            let fetchedEvents = WorldEventsStore.getSince dt store

            fetchedEvents
            |> should be (equivalent [firstEvent; secondEvent])

        [<Test>]
        let ``getting between first and second events returns second event only`` () =
            let dt = new DateTime(1100L)
            let fetchedEvents = WorldEventsStore.getSince dt store

            fetchedEvents
            |> should be (equivalent [secondEvent])

        [<Test>]
        let ``getting after second event returns empty`` () =
            let dt = new DateTime(1400L)
            let fetchedEvents = WorldEventsStore.getSince dt store

            fetchedEvents
            |> List.isEmpty
            |> should equal true

        [<TestFixture>]
        module ``adding a process result`` =
            let newWorld = World.empty [Rect.create 0 0 4 4]
            let event = WorldEvent.Moved (1, Direction.South, 1uy)

            let processResult: IntentionProcessing.ProcessResult =
                {
                    events = [event]
                    world = newWorld
                }

            let now = new DateTime(1400L)

            let newStore = WorldEventsStore.addResult processResult now store

            [<Test>]
            let ``new event is added to the store`` () =
                let matchingEvent =
                    newStore.recentEvents
                    |> List.tryFind (fun e -> e.worldEvent = event)

                matchingEvent.IsSome |> should equal true
                matchingEvent.Value.timestamp |> should equal now

            [<Test>]
            let ``world is updated`` () =
                newStore.world |> should equal newWorld

