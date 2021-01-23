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
            let guid = new Guid(bytes)
            DesrlResult.create guid size
