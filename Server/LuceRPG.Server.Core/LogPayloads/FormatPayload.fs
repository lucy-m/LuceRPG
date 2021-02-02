namespace LuceRPG.Server.Core

module FormatPayload =

    let format
            (payload: string seq)
            (subType: string)
            (eventType: string)
            (timestamp: int64)
            : string =
        let timestampStr = sprintf "%i" timestamp
        let payloadStr = payload |> String.concat ","

        seq { timestampStr; eventType; subType; payloadStr }
        |> String.concat ","
