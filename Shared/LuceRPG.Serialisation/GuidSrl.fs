namespace LuceRPG.Serialisation

open System

module GuidSrl =
    let size = 16

    let serialise (g: Guid): byte[] =
        g.ToByteArray()

    let deserialise (bytes: byte[]): Guid DesrlResult =
        if bytes.Length < size
        then Option.None
        else
            let guidBytes = bytes |> Array.take size
            let guid = new Guid(guidBytes)
            DesrlResult.create guid size
