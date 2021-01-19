namespace LuceRPG.Serialisation

module DesrlResult =
    type 'T Model =
        {
           value: 'T
           bytesRead: int
        }

    let create (value: 'T) (bytesRead: int): 'T Model Option =
        {
            value = value
            bytesRead = bytesRead
        }
        |> Option.Some

    let bind (tValue: 'T Option) (bytesRead: int): 'T Model Option =
        tValue
        |> Option.bind (fun value -> create value bytesRead)

    let value (model: 'T Model Option): 'T Option =
        model
        |> Option.map (fun m -> m.value)

type 'T DesrlResult = 'T DesrlResult.Model Option
