namespace LuceRPG.Models

module Id =
    type WorldObject = string
    type Intention = string

    let make (): string =
        System.Guid.NewGuid().ToString()
