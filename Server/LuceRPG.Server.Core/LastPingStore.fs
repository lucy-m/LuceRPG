namespace LuceRPG.Server.Core

open LuceRPG.Models

/// This stores all clients and their last ping timestamp
/// It can be culled, which removes all stale clients
///   and generates LeaveGame intentions
module LastPingStore =
    type Model = Map<Id.Client, int64>

    type CullResult =
        {
            intentions: Intention seq
            updated: Model
        }

    let cull (since: int64) (store: Model): CullResult =
        let staleClients, freshClients =
            store
            |> Map.partition (fun cId ts -> ts < since)

        let intentions =
            staleClients
            |> Map.toSeq
            |> Seq.map (fun (cId, ts) ->
                Intention.makePayload cId Intention.Type.LeaveGame
                |> WithId.create
            )

        {
            intentions = intentions
            updated = freshClients
        }
