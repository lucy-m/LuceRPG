namespace LuceRPG.Samples

open LuceRPG.Models

module SampleWorlds =
    let world1: World =
        let bounds = [
            Rect.create 0 0 14 8
            Rect.create 4 8 3 6
            Rect.create -6 -10 6 16
        ]

        let walls = [
            WorldObject.create WorldObject.Type.Wall (Point.create 2 2)
            WorldObject.create WorldObject.Type.Wall (Point.create 4 2)
            WorldObject.create WorldObject.Type.Wall (Point.create 7 2)
            WorldObject.create WorldObject.Type.Wall (Point.create 9 3)
            WorldObject.create WorldObject.Type.Wall (Point.create -4 -1)

            WorldObject.create WorldObject.Type.Player (Point.create 2 5)

            WorldObject.create (WorldObject.Type.Path (8,1)) (Point.create 1 1)
        ]

        World.createWithObjs bounds walls
