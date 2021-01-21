namespace LuceRPG.Serialisation

open System

module LongSrl =
    let size = 8

    let serialise (n: int64): byte[] =
        BitConverter.GetBytes(n)

    let deserialise (bytes: byte[]): int64 DesrlResult =
        if bytes.Length < size
        then Option.None
        else
            let value = BitConverter.ToInt64(bytes, 0)

            DesrlResult.create value size
