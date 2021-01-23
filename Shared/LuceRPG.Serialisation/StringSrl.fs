namespace LuceRPG.Serialisation

open System

module StringSrl =
    let encoding = Text.Encoding.ASCII

    let serialise (s: string): byte[] =
        if s = null
        then [||]
        else
            let count = encoding.GetByteCount(s) |> IntSrl.serialise
            let bytes = encoding.GetBytes(s)

            Array.append count bytes

    let deserialise (bytes: byte[]): string DesrlResult =
        let tCount, itemBytes = DesrlUtil.desrlAndSkip IntSrl.deserialise bytes

        tCount
        |> Option.bind (fun payload ->
            let count = payload.value

            if itemBytes.Length < count
            then Option.None
            else
                let value = encoding.GetString(itemBytes |> Array.take count)
                let bytesRead = payload.bytesRead + count

                DesrlResult.create value bytesRead
        )
