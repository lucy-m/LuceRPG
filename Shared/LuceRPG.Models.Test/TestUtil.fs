namespace LuceRPG.Models

module TestUtil =

    let withId (t: 'T): 'T WithId =
        let guid = System.Guid.NewGuid().ToString()

        WithId.useId guid t

    let makeId (): string =
        System.Guid.NewGuid().ToString()

    let makePlayer (topLeft: Point): WorldObject =
        let name = System.Guid.NewGuid().ToString()
        let playerData = PlayerData.create name
        let payload = WorldObject.create (WorldObject.Type.Player playerData) topLeft

        WithId.create payload
