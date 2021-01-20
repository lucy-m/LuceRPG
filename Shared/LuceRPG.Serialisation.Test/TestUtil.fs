namespace LuceRPG.Serialisation

module TestUtil =

    let srlAndDesrl
        (value: 'T)
        (srlFn: 'T -> byte[])
        (desrlFn: byte[] -> 'T DesrlResult)
        : bool =
            let srl = srlFn value
            let desrl = desrlFn srl |> DesrlResult.value

            desrl = Option.Some value
