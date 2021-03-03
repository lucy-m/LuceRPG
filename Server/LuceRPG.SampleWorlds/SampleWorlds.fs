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

        let harry =
            let charData =
                CharacterData.create
                    CharacterData.HairStyle.Long
                    CharacterData.HairColour.caramel
                    CharacterData.SkinColour.cocoa70
                    CharacterData.ClothingColour.grass
                    CharacterData.ClothingColour.grey
                    "Harry"
            let t = WorldObject.Type.NPC charData
            WorldObject.create t (Point.create 16 4) Direction.South
            |> WithId.create

        let barry =
            let charData =
                CharacterData.create
                    CharacterData.HairStyle.Short
                    CharacterData.HairColour.cocoa85
                    CharacterData.SkinColour.butter
                    CharacterData.ClothingColour.sky
                    CharacterData.ClothingColour.maroon
                    "Barry"

            let t = WorldObject.Type.NPC charData
            WorldObject.create t (Point.create 18 4) Direction.South
            |> WithId.create

        let garry =
            let charData =
                CharacterData.create
                    CharacterData.HairStyle.Egg
                    CharacterData.HairColour.tangerine
                    CharacterData.SkinColour.ivory
                    CharacterData.ClothingColour.grey
                    CharacterData.ClothingColour.maroon
                    "Garry"

            let t = WorldObject.Type.NPC charData
            WorldObject.create t (Point.create 20 4) Direction.South
            |> WithId.create

        let warp =
            let t = WorldObject.Type.Warp (world2Id, Point.zero)
            WorldObject.create t (Point.create -4 8) Direction.South
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
                WithId.create (wo  Direction.South)
            )

        let sayHiInteraction: Interaction =
            let sayHi = Interaction.One.Chat "Hey you, you're finally awake. Welcome to LuceRPG pre-alpha dev preview. Enjoy your stay, {player}!"
            let payload = [sayHi]
            WithId.create(payload)

        let interactionMap: World.InteractionMap =
            Map.ofList [harry.id, sayHiInteraction.id]

        let world =
            World.createWithInteractions
                "sampleville"
                bounds
                spawnPoint
                (harry::barry::garry::warp::walls)
                interactionMap
            |> WithId.useId world1Id

        let interactions = [sayHiInteraction]

        (world, interactions)

    let world2: (World * Interactions) =
        let bounds = [ Rect.create 0 0 8 8 ]
        let spawnPoint = Point.create 4 0

        let npc =
            let charData = CharacterData.randomized "Bobby"
            let t = WorldObject.Type.NPC charData
            WorldObject.create t (Point.create 6 4) Direction.South
            |> WithId.create

        let warp =
            let t = WorldObject.Type.Warp (world1Id, Point.create -4 6)
            WorldObject.create t (Point.create 1 6) Direction.South
            |> WithId.create

        let sayHiInteraction: Interaction =
            let sayHi = Interaction.One.Chat "Oh goodness, {player}! You found my secret hideout!"
            let payload = [sayHi]
            WithId.create(payload)

        let interactionMap: World.InteractionMap =
            Map.ofList [npc.id, sayHiInteraction.id]

        let world =
            World.createWithInteractions
                "world2"
                bounds
                spawnPoint
                [warp; npc]
                interactionMap
                |> WithId.useId world2Id

        let interactions = [sayHiInteraction]

        (world, interactions)

    let collection =
        WorldCollection.create
            (fst world1).id
            [world1; world2]
