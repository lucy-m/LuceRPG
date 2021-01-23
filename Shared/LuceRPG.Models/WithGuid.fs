namespace LuceRPG.Models

module WithGuid =
    type 'T Model =
        {
            id: System.Guid
            value: 'T
        }

    let create (id: System.Guid) (value: 'T): 'T Model =
        {
            id = id
            value = value
        }

type 'T WithGuid = 'T WithGuid.Model
