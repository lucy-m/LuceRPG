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
            WorldObject.create 1 WorldObject.Type.Wall (Point.create 2 2)
            WorldObject.create 2 WorldObject.Type.Wall (Point.create 4 2)
            WorldObject.create 3 WorldObject.Type.Wall (Point.create 7 2)
            WorldObject.create 4 WorldObject.Type.Wall (Point.create 9 3)
            WorldObject.create 5 WorldObject.Type.Wall (Point.create -4 -1)

            WorldObject.create 6 WorldObject.Type.Player (Point.create 2 5)

            WorldObject.create 7 (WorldObject.Type.Path (8,1)) (Point.create 1 1)
        ]

        World.createWithObjs bounds walls
