namespace LuceRPG.Models

module Id =
    type WorldObject = string
    type Intention = string
    type Client = string
    type Interaction = string
    type World = string

    let make (): string =
        System.Guid.NewGuid().ToString()
