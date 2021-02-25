namespace LuceRPG.Models

module TestUtil =

    let withId (t: 'T): 'T WithId =
        let guid = System.Guid.NewGuid().ToString()

        WithId.useId guid t

    let makePlayerWithName (btmLeft: Point) (name: string): WorldObject =
        let playerData = CharacterData.create name
        let payload = WorldObject.create (WorldObject.Type.Player playerData) btmLeft

        WithId.create payload

    let makePlayer (btmLeft: Point): WorldObject =
        let name = System.Guid.NewGuid().ToString()

        makePlayerWithName btmLeft name
