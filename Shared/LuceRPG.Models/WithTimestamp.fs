namespace LuceRPG.Models

module WithTimestamp =
    type 'T Model =
        {
            timestamp: int64
            value: 'T
        }

type 'T WithTimestamp = 'T WithTimestamp.Model
