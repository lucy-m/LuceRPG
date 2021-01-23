namespace LuceRPG.Serialisation

open LuceRPG.Models

module IntentionSrl =
    let serialisePayload (i: Intention.Payload): byte[] =
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

    let serialise (i: Intention): byte[] =
        WithGuidSrl.serialise serialisePayload i

    let deserialisePayload (bytes: byte[]): Intention.Payload DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): Intention.Payload DesrlResult =
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

    let deserialise (bytes: byte[]): Intention DesrlResult =
        WithGuidSrl.deserialise
            deserialisePayload
            bytes
