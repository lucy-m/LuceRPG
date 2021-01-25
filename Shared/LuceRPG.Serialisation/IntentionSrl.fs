namespace LuceRPG.Serialisation

open LuceRPG.Models

module IntentionSrl =
    let serialiseType (i: Intention.Type): byte[] =
        let label =
            match i with
            | Intention.Move  _-> 1uy
            | Intention.JoinGame -> 2uy
            | Intention.LeaveGame -> 3uy

        let addtInfo =
            match i with
            | Intention.Move (id, d, a) ->
                Array.concat [
                    (StringSrl.serialise id)
                    (DirectionSrl.serialise d)
                    (ByteSrl.serialise a)
                ]
            | Intention.JoinGame -> [||]
            | Intention.LeaveGame -> [||]

        Array.append [|label|] addtInfo

    let serialisePayload (i: Intention.Payload): byte[] =
        let clientId = StringSrl.serialise i.clientId
        let t = serialiseType i.t

        Array.append clientId t

    let serialise (i: Intention): byte[] =
        WithIdSrl.serialise serialisePayload i

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
                DesrlResult.create Intention.JoinGame 0
            | 3uy ->
                DesrlResult.create Intention.LeaveGame 0
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
