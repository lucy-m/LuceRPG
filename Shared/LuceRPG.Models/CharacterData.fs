namespace LuceRPG.Models

module CharacterData =
    type Model =
        {
            name: string
        }

    let create (name: string): Model =
        {
            name = name
        }

type CharacterData = CharacterData.Model
