﻿namespace LuceRPG.Models

module WithId =
    type 'T Model =
        {
            id: string
            value: 'T
        }

    let create (id: string) (value: 'T): 'T Model =
        {
            id = id
            value = value
        }

    let make (value: 'T): 'T Model =
        let id = Id.make()
        {
            id = id
            value = value
        }

type 'T WithId = 'T WithId.Model
