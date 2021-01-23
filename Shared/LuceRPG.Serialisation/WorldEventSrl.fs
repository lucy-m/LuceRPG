namespace LuceRPG.Serialisation

open LuceRPG.Models

module WorldEventSrl =
    let serialise (i: WorldEvent): byte[] =

        let label =
            match i with
            | WorldEvent.Moved  _-> 1uy

        let addtInfo =
            match i with
            | WorldEvent.Moved (id, d,a) ->
                Array.concat [
                    (StringSrl.serialise id)
                    (DirectionSrl.serialise d)
                    (ByteSrl.serialise a)
                ]

        Array.append [|label|] addtInfo

    let deserialise (bytes: byte[]): WorldEvent DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): WorldEvent DesrlResult =
            match tag with
            | 1uy ->
                DesrlUtil.getThree
                    StringSrl.deserialise
                    DirectionSrl.deserialise
                    ByteSrl.deserialise
                    (fun id d a -> WorldEvent.Moved (id, d,a))
                    objectBytes
            | _ ->
                printfn "Unknown WorldEvent tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes
