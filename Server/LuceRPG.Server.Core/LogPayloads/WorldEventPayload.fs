namespace LuceRPG.Server.Core

open LuceRPG.Models

module WorldEventPayload =

    let create (e: WorldEvent.Model): string seq =

        let worldEventType (t: WorldEvent.Type): string =
            match t with
            | WorldEvent.Type.Moved (id, dir) ->
                sprintf "Moved %c %s" (Direction.asLetter dir) id
            | WorldEvent.Type.ObjectAdded o ->
                sprintf "Added %s" o.id
            | WorldEvent.Type.ObjectRemoved id ->
                sprintf "Removed %s" id

        let resultOf = sprintf "Result of %s" e.resultOf
        let index = sprintf "Index %i" e.index
        let t = worldEventType e.t

        seq { "Event"; resultOf; index; t}
