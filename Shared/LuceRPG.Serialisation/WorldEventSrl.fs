﻿namespace LuceRPG.Serialisation

open LuceRPG.Models

module WorldEventSrl =
    let serialiseType (i: WorldEvent.Type): byte[] =

        let label =
            match i with
            | WorldEvent.Moved  _-> 1uy
            | WorldEvent.ObjectAdded _ -> 2uy

        let addtInfo =
            match i with
            | WorldEvent.Moved (id, d, a) ->
                Array.concat [
                    (StringSrl.serialise id)
                    (DirectionSrl.serialise d)
                    (ByteSrl.serialise a)
                ]
            | WorldEvent.ObjectAdded obj ->
                WorldObjectSrl.serialise obj

        Array.append [|label|] addtInfo

    let serialise (e: WorldEvent): byte[] =
        let id = StringSrl.serialise e.resultOf
        let payload = serialiseType e.t

        Array.append id payload

    let deserialiseType (bytes: byte[]): WorldEvent.Type DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): WorldEvent.Type DesrlResult =
            match tag with
            | 1uy ->
                DesrlUtil.getThree
                    StringSrl.deserialise
                    DirectionSrl.deserialise
                    ByteSrl.deserialise
                    (fun id d a -> WorldEvent.Type.Moved (id, d,a))
                    objectBytes
            | 2uy ->
                WorldObjectSrl.deserialise objectBytes
                |> DesrlResult.map (fun o -> WorldEvent.Type.ObjectAdded o)
            | _ ->
                printfn "Unknown WorldEvent tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes

    let deserialise (bytes: byte[]): WorldEvent DesrlResult =
        DesrlUtil.getTwo
            StringSrl.deserialise
            deserialiseType
            WorldEvent.asResult
            bytes
