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
            recentEvents: WorldEvent WithTimestamp List
            world: World
            objectClientMap: IntentionProcessing.ObjectClientMap
        }

    // So far the only way for ownership to be established is
    //   through intentions process results
    // A freshly loaded world will never have any ownership
    let create (world: World): Model =
        {
            lastCull = 0L
            recentEvents = []
            world = world
            objectClientMap = Map.empty
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

        let recentEvents = state.recentEvents @ storedEvents

        {
            lastCull = state.lastCull
            recentEvents = recentEvents
            world = result.world
            objectClientMap = result.objectClientMap
        }

    /// Returns recent events if available
    /// Returns whole world if events have been culled
    let getSince (timestamp: int64) (state: Model): GetSinceResult.Payload =
        if timestamp >= state.lastCull
        then
            state.recentEvents
            |> List.filter (fun e -> e.timestamp >= timestamp)
            |> GetSinceResult.Events
        else
            GetSinceResult.World state.world

    let cull (timestamp: int64) (state: Model): Model =
        let recentEvents =
            state.recentEvents
            |> List.filter (fun e -> e.timestamp >= timestamp)

        {
            lastCull = timestamp
            recentEvents = recentEvents
            world = state.world
            objectClientMap = state.objectClientMap
        }

type WorldEventsStore = WorldEventsStore.Model
