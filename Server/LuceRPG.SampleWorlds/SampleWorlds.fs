namespace LuceRPG.Samples

open LuceRPG.Models

module SampleWorlds =
    let world1: World =
        let bounds =
            [
                Rect.create 0 0 14 8
                Rect.create 4 8 3 8
                Rect.create -6 0 6 11
            ]

        let spawnPoint = Point.create 2 -5

        let walls =
            [
                WorldObject.create WorldObject.Type.Wall (Point.create 2 -2)
                WorldObject.create WorldObject.Type.Wall (Point.create 4 -2)
                WorldObject.create WorldObject.Type.Wall (Point.create 7 -2)
                WorldObject.create WorldObject.Type.Wall (Point.create 9 -3)
                WorldObject.create WorldObject.Type.Wall (Point.create -4 -1)

                WorldObject.create WorldObject.Type.Player (Point.create 2 -5)

                WorldObject.create (WorldObject.Type.Path (15,1)) (Point.create -2 -6)
            ]
            |> List.map (fun wo ->
                WithId.create wo
            )

        World.createWithObjs bounds spawnPoint walls
