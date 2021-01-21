namespace LuceRPG.Server.Core

open System
open LuceRPG.Models

/// This stores all events that occurred in the world
/// Later this can discard old events and for any
///   requests for before earlier events will return
///   world state
module WorldEventsStore =
    type StoredEvent =
        {
            timestamp: DateTime
            worldEvent: WorldEvent
        }

    type Model =
        {
            recentEvents: StoredEvent List
            world: World
        }

    let create (world: World): Model =
        {
            recentEvents = []
            world = world
        }

    let addResult (result: IntentionProcessing.ProcessResult) (now: DateTime) (state: Model): Model =
        let storedEvents =
            result.events
            |> List.map (fun e ->
                {
                    timestamp = now
                    worldEvent = e
                }
            )

        let recentEvents = state.recentEvents @ storedEvents

        {
            recentEvents = recentEvents
            world = result.world
        }

    let getSince (dateTime: DateTime) (state: Model): StoredEvent List =
        state.recentEvents
        |> List.filter (fun e -> e.timestamp >= dateTime)

type WorldEventsStore = WorldEventsStore.Model
