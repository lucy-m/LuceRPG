namespace LuceRPG.Serialisation

open System

module IntSrl =
    let size = 4

    let serialise (n: int): byte[] =
        BitConverter.GetBytes(n)

    let deserialise (bytes: byte[]): int DesrlResult =
        if bytes.Length < size
        then Option.None
        else
            let value = BitConverter.ToInt32(bytes, 0)

            DesrlResult.create value size
