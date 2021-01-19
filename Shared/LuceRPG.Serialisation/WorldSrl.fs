namespace LuceRPG.Serialisation

open LuceRPG.Models

module WorldSrl =
    let serialise (w: World): byte[] =
        let bounds =
            ListSrl.serialise
                RectSrl.serialise
                (Set.toList w.bounds)

        let objects =
            ListSrl.serialise
                WorldObjectSrl.serialise
                (Set.toList w.objects)

        Array.append bounds objects

    let deserialise (bytes: byte[]): World DesrlResult =
        let getBounds = ListSrl.deserialise RectSrl.deserialise
        let getObjects = ListSrl.deserialise WorldObjectSrl.deserialise

        let toWorld (bounds: Rect List) (objects: WorldObject List): World =
            let empty = World.empty bounds
            World.addObjects objects empty |> Option.defaultValue empty

        DesrlUtil.getTwo
            getBounds
            getObjects
            toWorld
            bytes
