namespace LuceRPG.Server.Core

open LuceRPG.Models

module IntentionFields =

    let create (ii: IndexedIntention): string seq =
        let id = sprintf "Id %s" ii.tsIntention.value.id
        let index = sprintf "Index %i" ii.index
        let world = sprintf "World %s" ii.worldId
        let clientId = sprintf "Client %s" ii.tsIntention.value.value.clientId

        let intentionType: string =
            match ii.tsIntention.value.value.t with
            | Intention.Type.JoinGame username -> sprintf "Join game %s" username
            | Intention.Type.JoinWorld obj -> sprintf "Join world %s" obj.id
            | Intention.Type.LeaveGame -> "Leave game"
            | Intention.Type.LeaveWorld -> "Leave world"
            | Intention.Type.Move (id, dir, amount) ->
                sprintf "Move %c %u %s" (Direction.asLetter dir) amount id
            | Intention.Type.TurnTowards (id, dir) ->
                sprintf "Turn towards %c %s" (Direction.asLetter dir) id
            | Intention.Type.Warp (target, id) ->
                match target with
                | Warp.Target.Dynamic (seed, dir, index) ->
                    sprintf "Warp dynamic %i %c %i %s" seed (Direction.asLetter dir) index id
                | Warp.Target.Static (worldId, point) ->
                    sprintf "Warp static to world %s (%i, %i) %s" worldId point.x point.y id

        seq { id; index; world; clientId; intentionType }
