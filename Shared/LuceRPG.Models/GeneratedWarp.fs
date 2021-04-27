namespace LuceRPG.Models

module GeneratedWarp =
    type Model =
        {
            toSeed: int
            direction: Direction
        }

    let createTarget (toSeed: int) (direction: Direction): Model =
        {
            toSeed = toSeed
            direction = direction
        }

