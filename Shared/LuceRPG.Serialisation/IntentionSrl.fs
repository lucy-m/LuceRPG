﻿namespace LuceRPG.Serialisation

open LuceRPG.Models

module IntentionSrl =
    let serialiseType (i: Intention.Type): byte[] =
        let label =
            match i with
            | Intention.Move  _-> 1uy
            | Intention.JoinGame _ -> 2uy
            | Intention.LeaveGame -> 3uy
            | Intention.JoinWorld _ -> 4uy
            | Intention.LeaveWorld -> 5uy
            | Intention.Warp _ -> 6uy
            | Intention.TurnTowards _ -> 7uy

        let addtInfo =
            match i with
            | Intention.Move (id, d, a) ->
                Array.concat [
                    (StringSrl.serialise id)
                    (DirectionSrl.serialise d)
                    (ByteSrl.serialise a)
                ]
            | Intention.JoinGame username -> StringSrl.serialise username
            | Intention.LeaveGame -> [||]
            | Intention.JoinWorld obj -> WorldObjectSrl.serialise obj
            | Intention.LeaveWorld -> [||]
            | Intention.Warp (warpTarget, objectId) ->
                Array.concat [
                    WarpSrl.serialiseTarget warpTarget
                    StringSrl.serialise objectId
                ]
            | Intention.TurnTowards (id, d) ->
                Array.concat [
                    (StringSrl.serialise id)
                    (DirectionSrl.serialise d)
                ]

        Array.append [|label|] addtInfo

    let serialisePayload (i: Intention.Payload): byte[] =
        let clientId = StringSrl.serialise i.clientId
        let t = serialiseType i.t

        Array.append clientId t

    let serialise (i: Intention): byte[] =
        WithIdSrl.serialise serialisePayload i

    let serialiseIndexed (ii: IndexedIntention): byte[] =
        let index = IntSrl.serialise ii.index
        let worldId = StringSrl.serialise ii.worldId
        let tsIntention = WithTimestampSrl.serialise serialise ii.tsIntention

        Array.concat [index; worldId; tsIntention]

    let deserialiseType (bytes: byte[]): Intention.Type DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): Intention.Type DesrlResult =
            match tag with
            | 1uy ->
                DesrlUtil.getThree
                    StringSrl.deserialise
                    DirectionSrl.deserialise
                    ByteSrl.deserialise
                    (fun id d a -> Intention.Move (id, d, a))
                    objectBytes
            | 2uy ->
                StringSrl.deserialise objectBytes
                |> DesrlResult.map Intention.JoinGame
            | 3uy ->
                DesrlResult.create Intention.LeaveGame 0
            | 4uy ->
                WorldObjectSrl.deserialise objectBytes
                |> DesrlResult.map Intention.JoinWorld
            | 5uy ->
                DesrlResult.create Intention.LeaveWorld 0
            | 6uy ->
                DesrlUtil.getTwo
                    WarpSrl.deserialiseTarget
                    StringSrl.deserialise
                    (fun warpTarget oId -> Intention.Warp(warpTarget, oId))
                    objectBytes
            | 7uy ->
                DesrlUtil.getTwo
                    StringSrl.deserialise
                    DirectionSrl.deserialise
                    (fun id d -> Intention.TurnTowards (id, d))
                    objectBytes
            | _ ->
                printfn "Unknown Intention tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes

    let deserialisePayload (bytes: byte[]): Intention.Payload DesrlResult =
        DesrlUtil.getTwo
            StringSrl.deserialise
            deserialiseType
            Intention.makePayload
            bytes

    let deserialise (bytes: byte[]): Intention DesrlResult =
        WithIdSrl.deserialise
            deserialisePayload
            bytes

    let deserialiseIndexed (bytes: byte[]): IndexedIntention DesrlResult =
        DesrlUtil.getThree
            IntSrl.deserialise
            StringSrl.deserialise
            (WithTimestampSrl.deserialise deserialise)
            IndexedIntention.useIndex
            bytes
