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
                Rect.create 4 8 24 10
                Rect.create -6 0 6 11
                Rect.create 2 -9 13 9
            ]

        let spawnPoint = Point.create 2 5

        let testWarps =
            let makeWarp (pos: Point) (direction: Direction)=
                Warp.create (Warp.createTarget "" Point.zero) Warp.Appearance.Mat
                |> WorldObject.Type.Warp
                |> fun t -> WorldObject.create t pos direction
                |> WithId.create

            [
                makeWarp (Point.create -4 3) Direction.South
                makeWarp (Point.create -5 4) Direction.West
                makeWarp (Point.create -2 4) Direction.East
                makeWarp (Point.create -4 6) Direction.North
            ]

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

        let inn =
            let warpData = Warp.createTarget world2Id (Point.create 5 0) |> Option.Some

            WorldObject.Type.Inn warpData
            |> fun t -> WorldObject.create t (Point.create 12 8) Direction.South
            |> WithId.create

        let nonInteractiveInn =
            WorldObject.Type.Inn Option.None
            |> fun t -> WorldObject.create t (Point.create 19 8) Direction.South
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

        let trees =
            [
                WorldObject.create WorldObject.Type.Tree (Point.create 6 9)
                WorldObject.create WorldObject.Type.Tree (Point.create 8 10)
                WorldObject.create WorldObject.Type.Tree (Point.create 9 11)
                WorldObject.create WorldObject.Type.Tree (Point.create 5 12)
                WorldObject.create WorldObject.Type.Tree (Point.create 10 15)
            ]
            |> List.map (fun wo ->
                WithId.create (wo  Direction.South)
            )

        let flowers =
            let rect = Rect.create 2 -9 13 8
            let points = Rect.getPoints rect

            points
            |> Seq.map (fun p ->
                Flower.randomized()
                |> WorldObject.Type.Flower
                |> fun t -> WorldObject.create t p Direction.South
                |> WithId.create
            )
            |> List.ofSeq

        let sayHiInteraction: Interaction =
            let sayHi = Interaction.One.Chat "Hey you, you're finally awake. Welcome to LuceRPG pre-alpha dev preview. Enjoy your stay, {player}!"
            let payload = [sayHi]
            WithId.create(payload)

        let interactionMap: World.InteractionMap =
            Map.ofList [harry.id, sayHiInteraction.id]

        let allObjects =
            List.concat
                [
                    [ harry; barry; garry; inn; nonInteractiveInn ]
                    walls
                    trees
                    testWarps
                    flowers
                ]

        let world =
            World.createWithInteractions
                "sampleville"
                bounds
                spawnPoint
                WorldBackground.GreenGrass
                allObjects
                interactionMap
            |> WithId.useId world1Id

        let interactions = [sayHiInteraction]

        (world, interactions)

    let world2: (World * Interactions) =
        let bounds = [ Rect.create 0 0 8 8; Rect.create 5 -1 2 1 ]
        let spawnPoint = Point.create 4 0

        let npc =
            let charData = CharacterData.randomized "Bobby"
            let t = WorldObject.Type.NPC charData
            WorldObject.create t (Point.create 6 4) Direction.South
            |> WithId.create

        let warp =
            let target = Warp.createTarget world1Id (Point.create 15 8)

            Warp.create target Warp.Appearance.Mat
            |> WorldObject.Type.Warp
            |> fun t -> WorldObject.create t (Point.create 5 -1) Direction.South
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
                WorldBackground.BrownPlanks
                [warp; npc]
                interactionMap
                |> WithId.useId world2Id

        let interactions = [sayHiInteraction]

        (world, interactions)

    let collection =
        WorldCollection.create
            (fst world1).id
            [world1; world2]
