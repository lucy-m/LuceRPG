﻿namespace LuceRPG.Serialisation

open LuceRPG.Models

module IntentionSrl =
    let serialise (i: Intention): byte[] =

        let label =
            match i with
            | Intention.Move  _-> 1uy

        let addtInfo =
            match i with
            | Intention.Move (id, d,a) ->
                Array.concat [
                    (IntSrl.serialise id)
                    (DirectionSrl.serialise d)
                    (ByteSrl.serialise a)
                ]

        Array.append [|label|] addtInfo

    let deserialise (bytes: byte[]): Intention DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): Intention DesrlResult =
            match tag with
            | 1uy ->
                DesrlUtil.getThree
                    IntSrl.deserialise
                    DirectionSrl.deserialise
                    ByteSrl.deserialise
                    (fun id d a -> Intention.Move (id, d,a))
                    objectBytes
            | _ ->
                printfn "Unknown Intention tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes