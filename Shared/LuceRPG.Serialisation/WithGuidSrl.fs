namespace LuceRPG.Serialisation

open LuceRPG.Models

module WithGuidSrl =
    let serialise (srlVal: 'T -> byte[]) (item: 'T WithGuid): byte[] =
        let id = GuidSrl.serialise item.id
        let value = srlVal item.value

        Array.append id value

    let deserialise
        (desrlVal: byte[] -> 'T DesrlResult)
        (bytes: byte[])
        : 'T WithGuid DesrlResult =
            DesrlUtil.getTwo
                GuidSrl.deserialise
                desrlVal
                WithGuid.create
                bytes
