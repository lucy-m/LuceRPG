namespace LuceRPG.Models

module PlayerData =
    type Model =
        {
            name: string
        }

    let create (name: string): Model =
        {
            name = name
        }

type PlayerData = PlayerData.Model
