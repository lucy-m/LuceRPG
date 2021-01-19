namespace LuceRPG.Serialisation

module DesrlUtil =
    let dsrlAndSkip
        (dsrl: byte[] -> 'T DesrlResult)
        (bytes: byte[])
        :'T DesrlResult * byte[] =
            let tDesrl = dsrl bytes

            let remaining =
                match tDesrl with
                | Option.None -> [||]
                | Option.Some desrl ->
                    Util.safeSkip desrl.bytesRead bytes

            (tDesrl, remaining)

    let getTwo
        (fn1: byte[] -> 'T1 DesrlResult)
        (fn2: byte[] -> 'T2 DesrlResult)
        (map: 'T1 -> 'T2 -> 'T3)
        (bytes: byte[])
        : 'T3 DesrlResult =
            let tt1, bytes1 = dsrlAndSkip fn1 bytes
            let tt2, _ = dsrlAndSkip fn2 bytes1

            match tt1, tt2 with
            | Option.Some t1, Option.Some t2 ->
                let value = map t1.value t2.value
                let bytesRead = t1.bytesRead + t2.bytesRead
                DesrlResult.create value bytesRead
            | _ -> Option.None
