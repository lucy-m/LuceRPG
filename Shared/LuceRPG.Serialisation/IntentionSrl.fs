namespace LuceRPG.Serialisation

open LuceRPG.Models

module IntentionSrl =
    let serialiseType (i: Intention.Type): byte[] =
        let label =
            match i with
            | Intention.Move  _-> 1uy
            | Intention.JoinGame _ -> 2uy
            | Intention.LeaveGame -> 3uy
            | Intention.JoinWorld _ -> 4uy

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
            | Intention.JoinWorld (wId, obj) ->
                Array.concat [
                    StringSrl.serialise wId
                    WorldObjectSrl.serialise obj
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
                DesrlUtil.getTwo
                    StringSrl.deserialise
                    WorldObjectSrl.deserialise
                    (fun wId obj -> Intention.JoinWorld (wId, obj))
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
