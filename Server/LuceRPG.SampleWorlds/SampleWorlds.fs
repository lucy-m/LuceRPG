namespace LuceRPG.Samples

open LuceRPG.Models

module SampleWorlds =
    let world1: (World * Interactions) =
        let bounds =
            [
                Rect.create 0 0 44 8
                Rect.create 4 8 3 8
                Rect.create -6 0 6 11
            ]

        let spawnPoint = Point.create 2 -5

        let npc =
            let playerData = PlayerData.create "Harry"
            let t = WorldObject.Type.NPC playerData
            WorldObject.create t (Point.create 16 -4)
            |> WithId.create

        let walls =
            [
                WorldObject.create WorldObject.Type.Wall (Point.create 2 -2)
                WorldObject.create WorldObject.Type.Wall (Point.create 4 -2)
                WorldObject.create WorldObject.Type.Wall (Point.create 7 -2)
                WorldObject.create WorldObject.Type.Wall (Point.create 9 -3)
                WorldObject.create WorldObject.Type.Wall (Point.create -4 -1)

                WorldObject.create (WorldObject.Type.Path (1,5)) (Point.create 5 8)
            ]
            |> List.map (fun wo ->
                WithId.create wo
            )

        let sayHiInteraction: Interaction =
            let sayHi = Interaction.One.Chat "Hey you, you're finally awake. Welcome to LuceRPG pre-alpha dev preview."
            let payload = [sayHi]
            WithId.create(payload)

        let interactionMap: World.InteractionMap =
            Map.ofList [npc.id, sayHiInteraction.id]

        let world = World.createWithInteractions bounds spawnPoint (npc::walls) interactionMap
        let interactions = [sayHiInteraction]

        (world, interactions)
