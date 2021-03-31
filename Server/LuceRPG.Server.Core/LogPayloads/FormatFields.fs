namespace LuceRPG.Server.Core

module FormatFields =

    let format
            (fields: string seq)
            (subType: string)
            (eventType: string)
            (timestamp: int64)
            : string =
        let formattedTimestamp = System.TimeSpan.FromTicks(timestamp).ToString("c")
        let timestampStr = sprintf "%i" timestamp
        let payloadStr = fields |> String.concat ","

        seq { formattedTimestamp; eventType; subType; payloadStr }
        |> String.concat ","
