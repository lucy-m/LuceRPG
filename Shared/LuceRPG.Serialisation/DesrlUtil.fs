namespace LuceRPG.Serialisation

module DesrlUtil =
    let desrlAndSkip
        (desrlFn: byte[] -> 'T DesrlResult)
        (bytes: byte[])
        :'T DesrlResult * byte[] =
            let tDesrl = desrlFn bytes

            let remaining =
                match tDesrl with
                | Option.None -> [||]
                | Option.Some desrl ->
                    Util.safeSkip desrl.bytesRead bytes

            (tDesrl, remaining)

    let getTwo
        (fn1: byte[] -> 'T1 DesrlResult)
        (fn2: byte[] -> 'T2 DesrlResult)
        (map: 'T1 -> 'T2 -> 'TR)
        (bytes: byte[])
        : 'TR DesrlResult =
            let tt1, bytes1 = desrlAndSkip fn1 bytes
            let tt2, _ = desrlAndSkip fn2 bytes1

            match tt1, tt2 with
            | Option.Some t1, Option.Some t2 ->
                let value = map t1.value t2.value
                let bytesRead = t1.bytesRead + t2.bytesRead
                DesrlResult.create value bytesRead
            | _ -> Option.None

    let getThree
        (fn1: byte[] -> 'T1 DesrlResult)
        (fn2: byte[] -> 'T2 DesrlResult)
        (fn3: byte[] -> 'T3 DesrlResult)
        (map: 'T1 -> 'T2 -> 'T3 -> 'TR)
        (bytes: byte[])
        : 'TR DesrlResult =
            let fn12 = getTwo fn1 fn2 (fun v1 v2 -> (v1, v2))

            getTwo
                fn12
                fn3
                (fun (v1,v2) v3 -> map v1 v2 v3)
                bytes

    let getFour
        (fn1: byte[] -> 'T1 DesrlResult)
        (fn2: byte[] -> 'T2 DesrlResult)
        (fn3: byte[] -> 'T3 DesrlResult)
        (fn4: byte[] -> 'T4 DesrlResult)
        (map: 'T1 -> 'T2 -> 'T3 -> 'T4 -> 'TR)
        (bytes: byte[])
        : 'TR DesrlResult =
            let fn12 = getTwo fn1 fn2 (fun v1 v2 -> (v1, v2))
            let fn34 = getTwo fn3 fn4 (fun v3 v4 -> (v3, v4))

            getTwo
                fn12
                fn34
                (fun (v1,v2) (v3,v4) -> map v1 v2 v3 v4)
                bytes

    let getTagged
        (fn: byte -> byte[] -> 'T DesrlResult)
        (bytes: byte[])
        : 'T DesrlResult =
            let tTag, itemBytes = desrlAndSkip ByteSrl.deserialise bytes

            tTag
            |> Option.bind (fun tag ->
                let value = fn tag.value itemBytes
                DesrlResult.addBytes tag.bytesRead value
            )
