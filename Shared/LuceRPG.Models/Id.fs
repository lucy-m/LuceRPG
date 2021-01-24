namespace LuceRPG.Models

module Id =
    type WorldObject = string
    type Intention = string
    type Client = string

    let make (): string =
        System.Guid.NewGuid().ToString()
