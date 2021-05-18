namespace LuceRPG.Server.Core

module FormatFields =

    let format
            (fields: string seq)
            (subType: string)
            (eventType: string)
            (timestamp: int64)
            : string =
        let formattedTimestamp = System.TimeSpan.FromTicks(timestamp).ToString("c")
        let payloadStr =
            Seq.append fields ["-";"-";"-";"-";"-"]
            |> Seq.take 5
            |> String.concat ","

        seq { formattedTimestamp; eventType; subType; payloadStr }
        |> String.concat ","

    let headers: string seq =
        seq { "Timestamp,Type,SubType,Field1,Field2,Field3,Field4,Field5" }
