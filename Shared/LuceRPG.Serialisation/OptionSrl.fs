namespace LuceRPG.Serialisation

module OptionSrl =

    let serialise
        (fn: 'T -> byte[])
        (t: 'T Option)
        : byte[] =
            let isSome = BoolSrl.serialise t.IsSome
            let itemBytes =
                match t with
                | Option.Some v -> fn v
                | Option.None -> [||]

            Array.append isSome itemBytes

    let deserialise
        (fn: byte[] -> 'T DesrlResult)
        (bytes: byte[])
        : ('T Option) DesrlResult =
            let tIsSome, itemBytes = DesrlUtil.desrlAndSkip BoolSrl.deserialise bytes

            tIsSome
            |> Option.bind (fun isSome ->
                if isSome.value
                then
                    let tValue =
                        fn itemBytes
                        |> DesrlResult.map Option.Some

                    DesrlResult.addBytes isSome.bytesRead tValue
                else
                    DesrlResult.create Option.None isSome.bytesRead
            )
