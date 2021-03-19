namespace LuceRPG.Serialisation

open System

module BoolSrl =
    let size = 1

    let serialise (b: bool): byte[] =
        BitConverter.GetBytes(b)

    let deserialise (bytes: byte[]): bool DesrlResult =
        if bytes.Length < size
        then Option.None
        else
            let value = BitConverter.ToBoolean(bytes, 0)

            DesrlResult.create value size
