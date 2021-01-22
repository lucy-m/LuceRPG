namespace LuceRPG.Serialisation

open LuceRPG.Models

module WithTimestampSrl =

    let serialise (srlVal: 'T -> byte[]) (te: 'T WithTimestamp): byte[] =
        let timestamp = LongSrl.serialise te.timestamp
        let value = srlVal te.value

        Array.append timestamp value

    let deserialise
        (desrlVal: byte[] -> 'T DesrlResult)
        (bytes: byte[])
        : 'T WithTimestamp DesrlResult =
            DesrlUtil.getTwo
                LongSrl.deserialise
                desrlVal
                (fun t v -> { timestamp = t; value = v })
                bytes
