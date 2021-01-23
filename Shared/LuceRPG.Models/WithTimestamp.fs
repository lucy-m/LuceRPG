namespace LuceRPG.Models

module WithTimestamp =
    type 'T Model =
        {
            timestamp: int64
            value: 'T
        }

    let create (timestamp: int64) (value: 'T): 'T Model =
        {
            timestamp = timestamp
            value = value
        }

type 'T WithTimestamp = 'T WithTimestamp.Model
