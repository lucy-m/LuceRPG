namespace LuceRPG.Server.Core

open NUnit.Framework
open FsUnit
open LuceRPG.Models
open System

[<TestFixture>]
module WorldEventsStore =
    let worldId = "world-id"
    let objId = System.Guid.NewGuid().ToString()

    let makeEvent (t: WorldEvent.Type): WorldEvent =
        let intentionId = System.Guid.NewGuid.ToString()
        WorldEvent.asResult intentionId worldId 0 t

    [<TestFixture>]
    module ``unculled store with two events`` =
        let firstEvent: WorldEvent WithTimestamp =
            {
                timestamp = 1000L
                value = WorldEvent.Moved (objId, Direction.North) |> makeEvent
            }

        let secondEvent: WorldEvent WithTimestamp =
            {
                timestamp = 1200L
                value = WorldEvent.Moved (objId, Direction.East) |> makeEvent
            }

        let events = [worldId, seq{firstEvent; secondEvent}] |> Map.ofList
        let world = World.empty "test" [] Point.zero
        let idWorld = world |> WithId.useId worldId
        let objectBusyMap = ["obj1", 1000L; "obj2", 1200L] |> Map.ofList
        let worldMap = [worldId, idWorld] |> Map.ofList

        let store: WorldEventsStore =
            {
                lastCull = 0L
                recentEvents = events
                worldMap = worldMap
                objectBusyMap = objectBusyMap
                serverSideData = ServerSideData.empty worldId
            }

        [<TestFixture>]
        module ``getResult`` =
            [<Test>]
            let ``getting before both events returns both events`` () =
                let dt = 800L
                let result = WorldEventsStore.getSince dt worldId store

                result |> should be (ofCase <@GetSinceResult.Events@>)

                match result with
                | GetSinceResult.Events es ->
                        es |> should be (equivalent [firstEvent; secondEvent])
                | _ -> failwith "Incorrect case"

            [<Test>]
            let ``getting between first and second events returns second event only`` () =
                let dt = 1100L
                let result = WorldEventsStore.getSince dt worldId store

                result |> should be (ofCase <@GetSinceResult.Events@>)

                match result with
                | GetSinceResult.Events es ->
                        es |> should be (equivalent [secondEvent])
                | _ -> failwith "Incorrect case"

            [<Test>]
            let ``getting after second event returns empty`` () =
                let dt = 1400L
                let result = WorldEventsStore.getSince dt worldId store

                result |> should be (ofCase <@GetSinceResult.Events@>)

                match result with
                | GetSinceResult.Events es ->
                        es |> should be (equivalent [])
                | _ -> failwith "Incorrect case"

        [<TestFixture>]
        module ``adding a process result`` =
            let newWorld = World.empty "test" [Rect.create 0 0 4 4] Point.zero |> WithId.useId worldId
            let newWorldMap = WithId.toMap [newWorld]
            let event = WorldEvent.Moved (objId, Direction.South) |> makeEvent
            let objectClientMap = Map.ofList ["obj1", "client1"]
            let wocm = [newWorld.id, objectClientMap] |> Map.ofList
            let objectBusyMap = Map.ofList ["obj1", 100L]
            let serverSideData = ServerSideData.create wocm Map.empty Map.empty worldId

            let processResult: IntentionProcessing.ProcessManyResult =
                {
                    events = [event]
                    delayed = []
                    worldMap = newWorldMap
                    objectBusyMap = objectBusyMap
                    serverSideData = serverSideData
                }

            let now = 1400L

            let newStore = WorldEventsStore.addResult processResult now store

            [<Test>]
            let ``new event is added to the store`` () =
                let matchingEvent =
                    newStore
                    |> WorldEventsStore.allRecentEvents
                    |> Seq.tryFind (fun e -> e.value = event)

                matchingEvent.IsSome |> should equal true
                matchingEvent.Value.timestamp |> should equal now

            [<Test>]
            let ``world is updated`` () =
                newStore.worldMap |> Map.containsKey worldId |> should equal true
                newStore.worldMap |> Map.find worldId |> should equal newWorld

            [<Test>]
            let ``objectClientMap is updated`` () =
                newStore.serverSideData.worldObjectClientMap |> should equal wocm

        [<TestFixture>]
        module ``culling`` =
            let cullTimestamp = 1100L
            let culledStore = WorldEventsStore.cull cullTimestamp store

            [<Test>]
            let ``removes old events`` () =
                culledStore
                |> WorldEventsStore.allRecentEvents
                |> should be (equivalent [secondEvent])

            [<Test>]
            let ``updates lastCull`` =
                culledStore.lastCull
                |> should equal cullTimestamp

            [<Test>]
            let ``world map is unchanged`` () =
                culledStore.worldMap |> should equal store.worldMap

            [<Test>]
            let ``updates object busy map`` () =
                let expected = ["obj2", 1200L]

                culledStore.objectBusyMap
                |> Map.toList
                |> should equal expected

    [<TestFixture>]
    module ``culled store`` =
        let world = World.empty "test" [] Point.zero |> WithId.useId worldId
        let worldMap = [worldId, world] |> Map.ofList
        let event: WorldEvent WithTimestamp =
            {
                timestamp = 1000L
                value = WorldEvent.Moved (objId, Direction.North) |> makeEvent
            }
        let recentEvents = [worldId, seq{event}] |> Map.ofList

        let cullTime = 800L

        let store: WorldEventsStore =
            {
                lastCull = cullTime
                recentEvents = recentEvents
                worldMap = worldMap
                objectBusyMap = Map.empty
                serverSideData = ServerSideData.empty worldId
            }

        [<Test>]
        let ``getting before cull returns world`` () =
            let result = WorldEventsStore.getSince 500L worldId store

            result |> should be (ofCase <@GetSinceResult.World@>)

            match result with
            | GetSinceResult.World w ->
                w |> should equal world
            | _ -> failwith "Incorrect case"

        [<Test>]
        let ``getting after cull returns events`` () =
            let result = WorldEventsStore.getSince 900L worldId store

            result |> should be (ofCase <@GetSinceResult.Events@>)

            match result with
            | GetSinceResult.Events es ->
                es |> should be (equivalent [event])
            | _ -> failwith "Incorrect case"
