namespace LuceRPG.Serialisation

module DesrlResult =
    type 'T Payload =
        {
           value: 'T
           bytesRead: int
        }

    type 'T Model = 'T Payload Option

    let create (value: 'T) (bytesRead: int): 'T Model =
        {
            value = value
            bytesRead = bytesRead
        }
        |> Option.Some

    let bind (tValue: 'T Option) (bytesRead: int): 'T Model =
        tValue
        |> Option.bind (fun value -> create value bytesRead)

    let value (model: 'T Model): 'T Option =
        model
        |> Option.map (fun m -> m.value)

    let addBytes (count: int) (model: 'T Model): 'T Model =
        model
        |> Option.bind (fun m -> create m.value (m.bytesRead + count))

type 'T DesrlResult = 'T DesrlResult.Model
