namespace LuceRPG.Samples

open LuceRPG.Models
open LuceRPG.Server.Core
open LuceRPG.Server.Core.WorldGenerator

module PrimordiaVille =
    let random = System.Random()

    module MapIds =
        let primordiaVilleOutside = System.Guid.NewGuid().ToString()
        let theThreeCocks = System.Guid.NewGuid().ToString()
        let barrysEssentials = System.Guid.NewGuid().ToString()
        let random = System.Guid.NewGuid().ToString()

    module NpcIds =
        let harry = System.Guid.NewGuid().ToString()
        let annie = System.Guid.NewGuid().ToString()
        let bob = System.Guid.NewGuid().ToString()
        let drWatson = System.Guid.NewGuid().ToString()
        let barry = System.Guid.NewGuid().ToString()

    module InteractionIds =
        let greeter = System.Guid.NewGuid().ToString()
        let gardener = System.Guid.NewGuid().ToString()
        let dontMindMe = System.Guid.NewGuid().ToString()

    let primordiaVilleOutside: (World * Interactions * BehaviourMap) =
        let bounds =
            [
                Rect.create 0 5 19 8
                Rect.create 7 13 12 7
                Rect.create 19 0 22 16
            ]

        let spawnPoint = Point.create 25 7

        let paths =
            [
                4,9,14,1
                16,10,2,5
                17,8,1,1
                17,7,21,1
                36,9,2,2
                25,0,2,7
            ]
            |> List.map (fun (x,y,w,h) ->
                WorldObject.create
                    (WorldObject.Type.Path (Point.create w h))
                    (Point.create x y)
                    Direction.South
                |> WithId.create
            )

        let trees =
            [
                0,12;   1,6;    4,11;   5,12;   6,7;    9,16
                11,14;  11,19;  15,7;   20,14;  22,15;  30,4
                32,0;   36,6;   38,1
            ]
            |> List.map (fun (x,y) ->
                WorldObject.create WorldObject.Type.Tree (Point.create x y) Direction.South
                |> WithId.create
            )

        let flowers =
            [
                5,10;   6,11;   8,14;   8,18;   9,14;   10,10
                13,12;  15,6;   16,6;   16,7;   27,6;   35,3
            ]
            |> List.map (fun (x,y) ->
                Flower.randomized()
                |> WorldObject.Type.Flower
                |> fun t -> WorldObject.create t (Point.create x y) Direction.South
                |> WithId.create
            )

        let flowerBeds =
            [
                Rect.create 20 1 5 5
                Rect.create 20 9 5 4
            ]
            |> List.collect (fun r ->
                Rect.getPoints r
                |> Seq.map (fun p ->
                    Flower.randomized()
                    |> WorldObject.Type.Flower
                    |> fun t -> WorldObject.create t p Direction.South
                    |> WithId.create
                )
                |> List.ofSeq
            )

        let inns =
            [
                13, 13, Option.Some (Warp.createTarget MapIds.theThreeCocks (Point.create 5 0))
                26, 9, Option.None
                33, 9, Option.Some (Warp.createTarget MapIds.barrysEssentials (Point.create 5 0))
            ]
            |> List.map (fun (x, y, warp) ->
                WorldObject.Type.Inn warp
                |> fun t -> WorldObject.create t (Point.create x y) Direction.South
                |> WithId.create
            )

        let warps =
            [ 25, 0, Warp.Dynamic (random.Next(), Direction.South) ]
            |> List.map (fun (x, y, warpTarget) ->
                Warp.create warpTarget Warp.Appearance.Mat
                |> WorldObject.Type.Warp
                |> fun t -> WorldObject.create t (Point.create x y) Direction.South
                |> WithId.create
            )

        let npcs =
            [
                9, 8, "Harry", NpcIds.harry
                22, 4, "Annie", NpcIds.annie
                39, 14, "Bob", NpcIds.bob
            ]
            |> List.map (fun (x, y, name, id) ->
                CharacterData.randomized name
                |> WorldObject.Type.NPC
                |> fun t -> WorldObject.create t (Point.create x y) Direction.South
                |> WithId.useId id
            )

        let interactions: Interaction List =
            [
                InteractionIds.greeter, "Hello, welcome to LuceRPG. This is PrimordiaVille. Please enjoy your stay."
                InteractionIds.gardener, "Hi, I'm the gardener of PrimordiaVille. I love growing flowers!"
                InteractionIds.dontMindMe, "Don't mind me, I'm just standing here doing absolutely nothing. Nothing to see here, nope."
            ]
            |> List.map (fun (id, text) ->
                Interaction.One.Chat text
                |> fun p -> [p]
                |> WithId.useId id
            )

        let interactionMap: World.InteractionMap =
            [
                NpcIds.harry, InteractionIds.greeter
                NpcIds.annie, InteractionIds.gardener
                NpcIds.bob, InteractionIds.dontMindMe
            ]
            |> Map.ofList

        let behaviourMap: BehaviourMap =
            let simpleSquare =
                Behaviour.patrolUniform
                    [
                        Direction.South, 2uy
                        Direction.West, 1uy
                        Direction.North, 2uy
                        Direction.East, 1uy
                    ]
                    (System.TimeSpan.FromSeconds(3.0))
                    true
                |> WithId.create

            let spinner =
                Behaviour.spinner (System.TimeSpan.FromSeconds(1.0))
                |> WithId.create

            let randomPatrol =
                Behaviour.randomWalk
                    System.TimeSpan.Zero
                    (System.TimeSpan.FromSeconds(5.0))
                |> WithId.create

            let backAndForth =
                Behaviour.patrolUniform
                    [
                        Direction.South, 8uy
                        Direction.North, 8uy
                    ]
                    (System.TimeSpan.FromSeconds(2.0))
                    true
                |> WithId.create

            [
                NpcIds.annie, randomPatrol
                NpcIds.harry, spinner
                NpcIds.bob, backAndForth
            ] |> Map.ofList

        let allObjects =
            List.concat
                [
                    paths
                    trees
                    flowers
                    flowerBeds
                    inns
                    warps
                    npcs
                ]

        let world =
            World.createWithInteractions
                "PrimordiaVille"
                bounds
                spawnPoint
                WorldBackground.GreenGrass
                allObjects
                interactionMap
            |> WithId.useId MapIds.primordiaVilleOutside

        (world, interactions, Map.empty)

    let theThreeCocks: (World * Interactions * BehaviourMap) =
        let bounds = [ Rect.create 0 0 8 8; Rect.create 5 -1 2 1 ]
        let spawnPoint = Point.create 4 0

        let drWatson =
            CharacterData.randomized "Dr Watson"
            |> WorldObject.Type.NPC
            |> fun t -> WorldObject.create t (Point.create 4 4) Direction.South
            |> WithId.useId NpcIds.drWatson

        let warp =
            Warp.createTarget MapIds.primordiaVilleOutside (Point.create 16 13)
            |> fun t -> Warp.create t Warp.Appearance.Mat
            |> WorldObject.Type.Warp
            |> fun t -> WorldObject.create t (Point.create 5 -1) Direction.South
            |> WithId.create

        let greetingInteraction: Interaction =
            "Hi {player}, welcome to The Three Cocks. Please excuse the lack of furniture, we have only just opened!"
            |> Interaction.One.Chat
            |> fun p -> [p]
            |> WithId.create

        let interactionMap: World.InteractionMap =
            [ drWatson.id, greetingInteraction.id ]
            |> Map.ofList

        let world =
            World.createWithInteractions
                "The Three Cocks"
                bounds
                spawnPoint
                WorldBackground.BrownPlanks
                [warp; drWatson]
                interactionMap
            |> WithId.useId MapIds.theThreeCocks

        (world, [greetingInteraction], Map.empty)

    let barrysEssentials: (World * Interactions * BehaviourMap) =
        let bounds = [ Rect.create 0 0 8 8; Rect.create 5 -1 2 1 ]
        let spawnPoint = Point.create 4 0

        let barry =
            CharacterData.randomized "Barry"
            |> WorldObject.Type.NPC
            |> fun t -> WorldObject.create t (Point.create 4 4) Direction.South
            |> WithId.useId NpcIds.barry

        let warp =
            Warp.createTarget MapIds.primordiaVilleOutside (Point.create 36 9)
            |> fun t -> Warp.create t Warp.Appearance.Mat
            |> WorldObject.Type.Warp
            |> fun t -> WorldObject.create t (Point.create 5 -1) Direction.South
            |> WithId.create

        let greetingInteraction: Interaction =
            "Hi {player}, this is Barry's Essentials. We sell only the essential items in here - which right now is absolutely nothing at all!"
            |> Interaction.One.Chat
            |> fun p -> [p]
            |> WithId.create

        let interactionMap: World.InteractionMap =
            [ barry.id, greetingInteraction.id ]
            |> Map.ofList

        let world =
            World.createWithInteractions
                "Barry's Essentials"
                bounds
                spawnPoint
                WorldBackground.BrownPlanks
                [warp; barry]
                interactionMap
            |> WithId.useId MapIds.barrysEssentials

        (world, [greetingInteraction], Map.empty)

    let collection =
        WorldCollection.create
            MapIds.primordiaVilleOutside
            [
                primordiaVilleOutside
                theThreeCocks
                barrysEssentials
            ]
