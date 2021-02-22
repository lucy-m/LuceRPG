namespace LuceRPG.Samples

open LuceRPG.Models
open LuceRPG.Server.Core

module SampleWorlds =
    let world1Id = System.Guid.NewGuid().ToString()
    let world2Id = System.Guid.NewGuid().ToString()

    let world1: (World * Interactions) =
        let bounds =
            [
                Rect.create 0 0 44 8
                Rect.create 4 8 3 8
                Rect.create -6 0 6 11
            ]

        let spawnPoint = Point.create 2 5

        let npc =
            let playerData = PlayerData.create "Harry"
            let t = WorldObject.Type.NPC playerData
            WorldObject.create t (Point.create 16 4)
            |> WithId.create

        let warp =
            let t = WorldObject.Type.Warp (world2Id, Point.zero)
            WorldObject.create t (Point.create -4 8)
            |> WithId.create

        let walls =
            [
                WorldObject.create WorldObject.Type.Wall (Point.create 2 2)
                WorldObject.create WorldObject.Type.Wall (Point.create 4 2)
                WorldObject.create WorldObject.Type.Wall (Point.create 7 2)
                WorldObject.create WorldObject.Type.Wall (Point.create 9 3)
                WorldObject.create WorldObject.Type.Wall (Point.create -4 1)
            ]
            |> List.map (fun wo ->
                WithId.create wo
            )

        let sayHiInteraction: Interaction =
            let sayHi = Interaction.One.Chat "Hey you, you're finally awake. Welcome to LuceRPG pre-alpha dev preview. Enjoy your stay, {player}!"
            let payload = [sayHi]
            WithId.create(payload)

        let interactionMap: World.InteractionMap =
            Map.ofList [npc.id, sayHiInteraction.id]

        let world =
            World.createWithInteractions
                "sampleville"
                bounds
                spawnPoint
                (npc::warp::walls)
                interactionMap
            |> WithId.useId world1Id

        let interactions = [sayHiInteraction]

        (world, interactions)

    let world2: (World * Interactions) =
        let bounds = [ Rect.create 0 0 16 16 ]
        let spawnPoint = Point.create 4 0

        let world = World.empty "world2" bounds spawnPoint |> WithId.useId world2Id
        let interactions = []

        (world, interactions)

    let collection =
        WorldCollection.create
            (fst world1).id
            [world1; world2]
