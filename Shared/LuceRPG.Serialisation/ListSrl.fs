namespace LuceRPG.Serialisation

module ListSrl =

    let serialise
        (fn: 'T -> byte[])
        (items: 'T List)
        : byte[] =
            let length = IntSrl.serialise items.Length
            let itemBytes =
                items
                |> Array.ofList
                |> Array.collect fn

            Array.append length itemBytes

    type 'T GetListAcc =
        {
            bytesRead: int
            items: 'T List
            nextBytes: byte[]
        }

    let deserialise
        (fn: byte[] -> 'T DesrlResult)
        (bytes: byte[])
        : ('T List) DesrlResult =
            let tSize, itemBytes = DesrlUtil.desrlAndSkip IntSrl.deserialise bytes

            let initial (bytesRead: int): 'T GetListAcc =
                {
                    bytesRead = bytesRead
                    items = []
                    nextBytes = itemBytes
                }

            let listFolder
                (tAcc: 'T GetListAcc Option)
                (index: int)
                : 'T GetListAcc Option =
                    tAcc
                    |> Option.bind (fun acc ->
                        let tDsrlResult, nextBytes = DesrlUtil.desrlAndSkip fn acc.nextBytes

                        let nextAcc =
                            tDsrlResult
                            |> Option.map (fun dsrlResult ->
                                let bytesRead = acc.bytesRead + dsrlResult.bytesRead
                                let items = dsrlResult.value :: acc.items

                                {
                                    bytesRead = bytesRead
                                    items = items
                                    nextBytes = nextBytes
                                }
                            )

                        nextAcc
                    )

            let tResult =
                tSize
                |> Option.bind (fun size ->
                    [1..size.value]
                    |> List.fold listFolder (Option.Some (initial size.bytesRead))
                )

            tResult
            |> Option.bind (fun result ->
                let value = result.items |> List.rev
                let bytesRead = result.bytesRead

                DesrlResult.create value bytesRead
            )
