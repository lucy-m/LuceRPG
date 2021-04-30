namespace LuceRPG.Server.Core

open LuceRPG.Models
open LuceRPG.Server.Core.WorldGenerator

/// This stores all events that occurred in the world
/// Later this can discard old events and for any
///   requests for before earlier events will return
///   world state
module WorldEventsStore =
    type Model =
        {
            lastCull: int64
            recentEvents: Map<Id.World, WorldEvent WithTimestamp seq>
            worldMap: Map<Id.World, World>
            interactionMap: Map<Id.World, Interactions>
            objectBusyMap: ObjectBusyMap
            serverSideData: ServerSideData
        }

    let allRecentEvents (store: Model): WorldEvent WithTimestamp seq =
        store.recentEvents
        |> Map.toSeq
        |> Seq.collect snd

    let allWorlds (store: Model): World seq =
        store.worldMap
        |> Map.toSeq
        |> Seq.map snd

    // So far the only way for ownership to be established is
    //   through intentions process results
    // A freshly loaded world will never have any ownership
    let create (worlds: WorldCollection): Model =
        let worldMap = worlds.allWorlds |> Seq.map (fun (w, i, b) -> (w.id, w)) |> Map.ofSeq
        let interactionMap = worlds.allWorlds |> Seq.map (fun (w, i, b) -> (w.id, i)) |> Map.ofSeq

        {
            lastCull = 0L
            recentEvents = Map.empty
            worldMap = worldMap
            interactionMap = interactionMap
            objectBusyMap = Map.empty
            serverSideData = ServerSideData.empty worlds.defaultWorld
        }

    let addResult (result: IntentionProcessing.ProcessManyResult) (now: int64) (state: Model): Model =
        let storedEvents =
            result.events
            |> Seq.map (fun e ->
                {
                    WithTimestamp.timestamp = now
                    WithTimestamp.value = e
                }
            )
            |> List.ofSeq

        let recentEvents =
            storedEvents
            |> Seq.fold (fun acc e ->
                let existingForWorld =
                    acc
                    |> Map.tryFind e.value.world
                    |> Option.defaultValue Seq.empty

                let withEvent = existingForWorld |> Seq.append [e]

                acc
                |> Map.add e.value.world withEvent
            ) state.recentEvents

        {
            lastCull = state.lastCull
            recentEvents = recentEvents
            worldMap = result.worldMap
            interactionMap = state.interactionMap
            objectBusyMap = result.objectBusyMap
            serverSideData = result.serverSideData
        }

    /// Returns recent events if available
    /// Returns whole world if events have been culled
    ///   or if the client has changed world
    let getSince
            (timestamp: int64)
            (clientId: Id.Client)
            (state: Model)
            : GetSinceResult.Payload =
        let tWorldId = state.serverSideData.clientWorldMap |> Map.tryFind clientId
        let tWorld =
            tWorldId
            |> Option.bind (fun worldId ->
                state.worldMap |> Map.tryFind worldId
            )

        match tWorld with
        | Option.None -> GetSinceResult.Failure (sprintf "Unknown world for client id %s" clientId)
        | Option.Some world ->
            if timestamp >= state.lastCull
            then
                let events =
                    state.recentEvents
                    |> Map.tryFind world.id
                    |> Option.defaultValue Seq.empty
                    |> Seq.filter (fun e -> e.timestamp >= timestamp)
                    |> List.ofSeq

                let worldChanges =
                    events
                    |> List.choose (fun e ->
                        match e.value.t with
                        | WorldEvent.Type.JoinedWorld cId -> Option.Some cId
                        | _ -> Option.None
                    )
                    |> List.filter (fun cId -> clientId = cId)

                if worldChanges |> List.isEmpty
                then GetSinceResult.Events events
                else
                    let interactions =
                        state.interactionMap
                        |> Map.tryFind world.id
                        |> Option.defaultValue []

                    GetSinceResult.WorldChanged (world, interactions)
            else
                GetSinceResult.World world

    let cull (timestamp: int64) (state: Model): Model =
        let recentEvents =
            state.recentEvents
            |> Map.map (fun worldId events ->
                events |> Seq.filter (fun e -> e.timestamp >= timestamp)
            )

        let culledBusyMap =
            state.objectBusyMap
            |> Map.filter (fun id until -> until >= timestamp)

        {
            lastCull = timestamp
            recentEvents = recentEvents
            worldMap = state.worldMap
            interactionMap = state.interactionMap
            objectBusyMap = culledBusyMap
            serverSideData = state.serverSideData
        }

    let generate (seed: int) (inDirection: Direction) (state: Model): Model =
        if state.serverSideData.generatedWorldMap |> Map.containsKey seed
        then state
        else
            let outDirection = Direction.inverse inDirection
            let parameters: WorldGenerator.Parameters =
                {
                    bounds = Rect.create 0 0 6 6
                    eccs = [outDirection, ExternalCountConstraint.Between(1,4)] |> Map.ofList
                    tileSet = Option.None
                }

            let world = WorldGenerator.generate parameters seed

            let spawnPoint =
                world.value.dynamicWarps
                |> Map.toSeq
                |> Seq.filter (fun (p, d) -> d = outDirection)
                |> Seq.tryHead
                |> Option.map (fun (p, d) -> Direction.movePoint inDirection 1 p)
                |> Option.defaultValue Point.zero

            let worldMap = state.worldMap |> Map.add world.id world
            let generatedWorldMap =
                state.serverSideData.generatedWorldMap |> Map.add seed (world.id, spawnPoint)
            let serverSideData =
                {
                    state.serverSideData
                        with generatedWorldMap = generatedWorldMap
                }

            {
                state with
                    worldMap = worldMap
                    serverSideData = serverSideData
            }

type WorldEventsStore = WorldEventsStore.Model
