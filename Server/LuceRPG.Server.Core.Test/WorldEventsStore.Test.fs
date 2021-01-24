namespace LuceRPG.Server.Core

open NUnit.Framework
open FsUnit
open LuceRPG.Models
open System

[<TestFixture>]
module WorldEventsStore =

    let objId = System.Guid.NewGuid().ToString()

    let makeEvent (t: WorldEvent.Type): WorldEvent =
        let intentionId = System.Guid.NewGuid.ToString()
        WorldEvent.asResult intentionId t

    [<TestFixture>]
    module ``unculled store with two events`` =
        let firstEvent: WorldEvent WithTimestamp =
            {
                timestamp = 1000L
                value = WorldEvent.Moved (objId, Direction.North, 1uy) |> makeEvent
            }

        let secondEvent: WorldEvent WithTimestamp =
            {
                timestamp = 1200L
                value = WorldEvent.Moved (objId, Direction.East, 1uy) |> makeEvent
            }

        let events = [firstEvent; secondEvent]
        let world = World.empty [] Point.zero

        let store: WorldEventsStore =
            {
                lastCull = 0L
                recentEvents = events
                world = world
                objectClientMap = Map.empty
            }

        [<TestFixture>]
        module ``getResult`` =
            [<Test>]
            let ``getting before both events returns both events`` () =
                let dt = 800L
                let result = WorldEventsStore.getSince dt store

                result |> should be (ofCase <@GetSinceResult.Events@>)

                match result with
                | GetSinceResult.Events es ->
                        es |> should be (equivalent [firstEvent; secondEvent])
                | _ -> failwith "Incorrect case"

            [<Test>]
            let ``getting between first and second events returns second event only`` () =
                let dt = 1100L
                let result = WorldEventsStore.getSince dt store

                result |> should be (ofCase <@GetSinceResult.Events@>)

                match result with
                | GetSinceResult.Events es ->
                        es |> should be (equivalent [secondEvent])
                | _ -> failwith "Incorrect case"

            [<Test>]
            let ``getting after second event returns empty`` () =
                let dt = 1400L
                let result = WorldEventsStore.getSince dt store

                result |> should be (ofCase <@GetSinceResult.Events@>)

                match result with
                | GetSinceResult.Events es ->
                        es |> should be (equivalent [])
                | _ -> failwith "Incorrect case"

        [<TestFixture>]
        module ``adding a process result`` =
            let newWorld = World.empty [Rect.create 0 0 4 4] Point.zero
            let event = WorldEvent.Moved (objId, Direction.South, 1uy) |> makeEvent
            let objectClientMap = Map.ofList ["obj1", "client1"]

            let processResult: IntentionProcessing.ProcessResult =
                {
                    events = [event]
                    world = newWorld
                    objectClientMap = objectClientMap
                }

            let now = 1400L

            let newStore = WorldEventsStore.addResult processResult now store

            [<Test>]
            let ``new event is added to the store`` () =
                let matchingEvent =
                    newStore.recentEvents
                    |> List.tryFind (fun e -> e.value = event)

                matchingEvent.IsSome |> should equal true
                matchingEvent.Value.timestamp |> should equal now

            [<Test>]
            let ``world is updated`` () =
                newStore.world |> should equal newWorld

            [<Test>]
            let ``objectClientMap is updated`` () =
                newStore.objectClientMap |> should equal objectClientMap

        [<TestFixture>]
        module ``culling`` =
            let cullTimestamp = 1100L
            let culledStore = WorldEventsStore.cull cullTimestamp store

            [<Test>]
            let ``removes old events`` =
                culledStore.recentEvents
                |> should be (equivalent [secondEvent])

            [<Test>]
            let ``updates lastCull`` =
                culledStore.lastCull
                |> should equal cullTimestamp

            [<Test>]
            let ``world is unchanged`` =
                culledStore.world
                |> should equal store.world

    [<TestFixture>]
    module ``culled store`` =
        let world = World.empty [] Point.zero
        let event: WorldEvent WithTimestamp =
            {
                timestamp = 1000L
                value = WorldEvent.Moved (objId, Direction.North, 1uy) |> makeEvent
            }

        let cullTime = 800L

        let store: WorldEventsStore =
            {
                lastCull = cullTime
                recentEvents = [event]
                world = world
                objectClientMap = Map.empty
            }

        [<Test>]
        let ``getting before cull returns world`` () =
            let result = WorldEventsStore.getSince 500L store

            result |> should be (ofCase <@GetSinceResult.World@>)

            match result with
            | GetSinceResult.World w ->
                w |> should equal world
            | _ -> failwith "Incorrect case"

        [<Test>]
        let ``getting after cull returns events`` () =
            let result = WorldEventsStore.getSince 900L store

            result |> should be (ofCase <@GetSinceResult.Events@>)

            match result with
            | GetSinceResult.Events es ->
                es |> should be (equivalent [event])
            | _ -> failwith "Incorrect case"
