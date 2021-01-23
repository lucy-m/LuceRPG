namespace LuceRPG.Models

module Id =
    type WorldObject = string

    let make (): string =
        System.Guid.NewGuid().ToString()
