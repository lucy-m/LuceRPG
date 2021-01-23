namespace LuceRPG.Serialisation

module ByteSrl =
    let size = 1

    let serialise (b: byte): byte[] =
        [|b|]

    let deserialise (bytes: byte[]): byte DesrlResult =
        if bytes.Length < size
        then Option.None
        else
            let value = Array.get bytes 0
            DesrlResult.create value size
