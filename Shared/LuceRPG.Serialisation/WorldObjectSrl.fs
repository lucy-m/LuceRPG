﻿namespace LuceRPG.Serialisation

open LuceRPG.Models

module WorldObjectSrl =
    let serialiseType (t: WorldObject.Type): byte[] =
        let label =
            match t with
            | WorldObject.Type.Wall -> 1uy
            | WorldObject.Type.Path _ -> 2uy
            | WorldObject.Type.Player -> 3uy

        let addtInfo =
            match t with
            | WorldObject.Type.Wall -> [||]
            | WorldObject.Type.Path (w,h) ->
                Array.append (IntSrl.serialise w) (IntSrl.serialise h)
            | WorldObject.Type.Player -> [||]

        Array.append [|label|] addtInfo

    let deserialiseType (bytes: byte[]): WorldObject.Type DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): WorldObject.Type DesrlResult =
            match tag with
            | 1uy -> DesrlResult.create WorldObject.Type.Wall 0
            | 2uy ->
                DesrlUtil.getTwo
                    IntSrl.deserialise
                    IntSrl.deserialise
                    (fun w h -> WorldObject.Type.Path (w,h))
                    objectBytes
            | 3uy -> DesrlResult.create WorldObject.Type.Player 0
            | _ ->
                printfn "Unknown WorldObject Type tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes

    let serialisePayload (obj: WorldObject.Payload): byte[] =
        let t = serialiseType obj.t
        let topLeft = PointSrl.serialise obj.topLeft

        Array.concat  [t; topLeft]

    let deserialisePayload (bytes: byte[]): WorldObject.Payload DesrlResult =
        DesrlUtil.getTwo
            deserialiseType
            PointSrl.deserialise
            WorldObject.create
            bytes

    let serialise (obj: WorldObject): byte[] =
        WithIdSrl.serialise serialisePayload obj

    let deserialise (bytes: byte[]): WorldObject DesrlResult =
        WithIdSrl.deserialise deserialisePayload bytes
