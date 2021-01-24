namespace LuceRPG.Serialisation

open LuceRPG.Models

module WithIdSrl =
    let serialise (srlVal: 'T -> byte[]) (item: 'T WithId): byte[] =
        let id = StringSrl.serialise item.id
        let value = srlVal item.value

        Array.append id value

    let deserialise
        (desrlVal: byte[] -> 'T DesrlResult)
        (bytes: byte[])
        : 'T WithId DesrlResult =
            DesrlUtil.getTwo
                StringSrl.deserialise
                desrlVal
                WithId.useId
                bytes
