namespace LuceRPG.Server.Core

open LuceRPG.Models

module ToLogString =

    let formatPayload (timestamp: int64) (eventType: string) (payload: string seq): string =
        let timestampStr = sprintf "%i" timestamp
        let payloadStr = payload |> String.concat ","

        seq { timestampStr; eventType; payloadStr }
        |> String.concat ","

    let direction (d: Direction): string =
        match d with
        | Direction.North -> "N"
        | Direction.South -> "S"
        | Direction.East -> "E"
        | Direction.West -> "W"

    let worldEventType (t: WorldEvent.Type): string =
        match t with
        | WorldEvent.Type.Moved (id, dir) ->
            sprintf "Moved %s %s" (direction dir) id
        | WorldEvent.Type.ObjectAdded o ->
            sprintf "Added %s" o.id
        | WorldEvent.Type.ObjectRemoved id ->
            sprintf "Removed %s" id

    let processResult
            (timestamp: int64)
            (result: IntentionProcessing.ProcessResult)
            : string seq =

        let typeStr = "Process Result"

        let events =
            result.events
            |> Seq.map (fun e ->
                let resultOf = sprintf "Result of %s" e.resultOf
                let index = sprintf "Index %i" e.index
                let t = worldEventType e.t

                seq { "Event"; resultOf; index; t}
            )

        let delayed =
            result.delayed
            |> Seq.map (fun d ->
                let id = sprintf "Id %s" d.tsIntention.value.id
                let index = sprintf "Index %i" d.index

                seq { "Delayed"; id; index }
            )

        let logs =
            (Seq.append events delayed)
            |> Seq.map (formatPayload timestamp typeStr)

        logs

    let clientJoined
            (timestamp: int64)
            (clientId: string)
            (username: string)
            : string =

        let payload = seq {
            sprintf "ClientId %s" clientId
            sprintf "Username %s" username
        }

        formatPayload timestamp "Client Joined" payload
