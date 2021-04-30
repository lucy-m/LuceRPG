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

    let getFive
        (fn1: byte[] -> 'T1 DesrlResult)
        (fn2: byte[] -> 'T2 DesrlResult)
        (fn3: byte[] -> 'T3 DesrlResult)
        (fn4: byte[] -> 'T4 DesrlResult)
        (fn5: byte[] -> 'T5 DesrlResult)
        (map: 'T1 -> 'T2 -> 'T3 -> 'T4 -> 'T5 -> 'TR)
        (bytes: byte[])
        : 'TR DesrlResult =
            let fn123 = getThree fn1 fn2 fn3 (fun v1 v2 v3 -> (v1, v2, v3))
            let fn45 = getTwo fn4 fn5 (fun v4 v5 -> (v4, v5))

            getTwo
                fn123
                fn45
                (fun (v1,v2,v3) (v4,v5) -> map v1 v2 v3 v4 v5)
                bytes

    let getSix
        (fn1: byte[] -> 'T1 DesrlResult)
        (fn2: byte[] -> 'T2 DesrlResult)
        (fn3: byte[] -> 'T3 DesrlResult)
        (fn4: byte[] -> 'T4 DesrlResult)
        (fn5: byte[] -> 'T5 DesrlResult)
        (fn6: byte[] -> 'T6 DesrlResult)
        (map: 'T1 -> 'T2 -> 'T3 -> 'T4 -> 'T5 -> 'T6 -> 'TR)
        (bytes: byte[])
        : 'TR DesrlResult =
            let fn123 = getThree fn1 fn2 fn3 (fun v1 v2 v3 -> (v1, v2, v3))
            let fn456 = getThree fn4 fn5 fn6 (fun v4 v5 v6 -> (v4, v5, v6))

            getTwo
                fn123
                fn456
                (fun (v1,v2,v3) (v4,v5,v6) -> map v1 v2 v3 v4 v5 v6)
                bytes

    let getSeven
        (fn1: byte[] -> 'T1 DesrlResult)
        (fn2: byte[] -> 'T2 DesrlResult)
        (fn3: byte[] -> 'T3 DesrlResult)
        (fn4: byte[] -> 'T4 DesrlResult)
        (fn5: byte[] -> 'T5 DesrlResult)
        (fn6: byte[] -> 'T6 DesrlResult)
        (fn7: byte[] -> 'T7 DesrlResult)
        (map: 'T1 -> 'T2 -> 'T3 -> 'T4 -> 'T5 -> 'T6 -> 'T7 -> 'TR)
        (bytes: byte[])
        : 'TR DesrlResult =
            let fn123 = getThree fn1 fn2 fn3 (fun v1 v2 v3 -> (v1, v2, v3))
            let fn45 = getTwo fn4 fn5 (fun v4 v5 -> (v4, v5))
            let fn67 = getTwo fn6 fn7 (fun v6 v7 -> (v6, v7))

            getThree
                fn123
                fn45
                fn67
                (fun (v1,v2,v3) (v4,v5) (v6, v7) -> map v1 v2 v3 v4 v5 v6 v7)
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
