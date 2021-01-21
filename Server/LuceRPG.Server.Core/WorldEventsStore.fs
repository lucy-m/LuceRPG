﻿namespace LuceRPG.Server.Core

open LuceRPG.Models

/// This stores all events that occurred in the world
/// Later this can discard old events and for any
///   requests for before earlier events will return
///   world state
module WorldEventsStore =
    type StoredEvent =
        {
            timestamp: int64
            worldEvent: WorldEvent
        }

    type Model =
        {
            lastCull: int64
            recentEvents: StoredEvent List
            world: World
        }

    let create (world: World): Model =
        {
            lastCull = 0L
            recentEvents = []
            world = world
        }

    let addResult (result: IntentionProcessing.ProcessResult) (now: int64) (state: Model): Model =
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
            lastCull = state.lastCull
            recentEvents = recentEvents
            world = result.world
        }

    type GetSinceResult =
        | Events of StoredEvent List
        | World of World

    /// Returns recent events if available
    /// Returns whole world if events have been culled
    let getSince (timestamp: int64) (state: Model): GetSinceResult =
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
        }

type WorldEventsStore = WorldEventsStore.Model
