namespace LuceRPG.Server.Core

open LuceRPG.Models

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
            objectBusyMap: IntentionProcessing.ObjectBusyMap
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
        let worldMap = worlds.allWorlds |> Seq.map (fun (w, i) -> (w.id, w)) |> Map.ofSeq

        {
            lastCull = 0L
            recentEvents = Map.empty
            worldMap = worldMap
            objectBusyMap = Map.empty
            serverSideData = ServerSideData.empty worlds.defaultWorld
        }

    let addResult (result: IntentionProcessing.ProcessResult) (now: int64) (state: Model): Model =
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

        let worldMap = state.worldMap |> Map.add result.world.id result.world

        let serverSideData = result.serverSideData |> Option.defaultValue state.serverSideData

        {
            lastCull = state.lastCull
            recentEvents = recentEvents
            worldMap = worldMap
            objectBusyMap = result.objectBusyMap
            serverSideData = serverSideData
        }

    /// Returns recent events if available
    /// Returns whole world if events have been culled
    let getSince (timestamp: int64) (worldId: Id.World) (state: Model): GetSinceResult.Payload =
        let tWorld = state.worldMap |> Map.tryFind worldId

        match tWorld with
        | Option.None -> GetSinceResult.Failure (sprintf "Unknown world id %s" worldId)
        | Option.Some world ->
            if timestamp >= state.lastCull
            then
                state.recentEvents
                |> Map.tryFind worldId
                |> Option.defaultValue Seq.empty
                |> Seq.filter (fun e -> e.timestamp >= timestamp)
                |> List.ofSeq
                |> GetSinceResult.Events
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
            objectBusyMap = culledBusyMap
            serverSideData = state.serverSideData
        }

type WorldEventsStore = WorldEventsStore.Model
