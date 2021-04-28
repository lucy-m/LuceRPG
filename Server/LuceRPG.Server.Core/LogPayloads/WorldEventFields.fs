namespace LuceRPG.Server.Core

open LuceRPG.Models

module WorldEventFields =

    let create (e: WorldEvent.Model): string seq =

        let worldEventType (t: WorldEvent.Type): string =
            match t with
            | WorldEvent.Type.Moved (id, dir) ->
                sprintf "Moved %c %s" (Direction.asLetter dir) id
            | WorldEvent.Type.TurnedTowards (id, dir) ->
                sprintf "Turned to %c %s" (Direction.asLetter dir) id
            | WorldEvent.Type.ObjectAdded o ->
                sprintf "Added %s" o.id
            | WorldEvent.Type.ObjectRemoved id ->
                sprintf "Removed %s" id
            | WorldEvent.Type.JoinedWorld cId ->
                sprintf "Client joined %s" cId
            | WorldEvent.Type.WorldGenerateRequest (seed, dir) ->
                sprintf "World generation requested %i %c" seed (Direction.asLetter dir)

        let resultOf = sprintf "Result of %s" e.resultOf
        let index = sprintf "Index %i" e.index
        let t = worldEventType e.t

        seq { "Event"; resultOf; index; t}
