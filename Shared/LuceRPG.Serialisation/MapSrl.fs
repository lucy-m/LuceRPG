namespace LuceRPG.Serialisation

module MapSrl =

    let serialise
            (fnKey: 'TKey -> byte[])
            (fnVal: 'TVal -> byte[])
            (items: Map<'TKey, 'TVal>)
            : byte[] =
        let serialiseEntry ((k, v): 'TKey * 'TVal): byte[] =
            let keyBytes = fnKey k
            let valBytes = fnVal v
            Array.append keyBytes valBytes

        let itemList = items |> Map.toList

        ListSrl.serialise serialiseEntry itemList

    let deserialise
            (fnKey: byte[] -> 'TKey DesrlResult)
            (fnVal: byte[] -> 'TVal DesrlResult)
            (bytes: byte[])
            : Map<'TKey, 'TVal> DesrlResult =
        let deserialiseEntry (objectBytes: byte[]): ('TKey * 'TVal) DesrlResult =
            DesrlUtil.getTwo
                fnKey
                fnVal
                (fun k v -> (k,v))
                objectBytes

        ListSrl.deserialise deserialiseEntry bytes
        |> DesrlResult.map Map.ofList
