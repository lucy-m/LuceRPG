namespace LuceRPG.Models

module TestUtil =

    let withId (t: 'T): 'T WithId =
        let guid = System.Guid.NewGuid().ToString()

        WithId.useId guid t

    let makePlayer (btmLeft: Point): WorldObject =
        let name = System.Guid.NewGuid().ToString()
        let playerData = PlayerData.create name
        let payload = WorldObject.create (WorldObject.Type.Player playerData) btmLeft

        WithId.create payload
