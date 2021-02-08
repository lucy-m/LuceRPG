namespace LuceRPG.Serialisation

open LuceRPG.Models

module WorldSrl =
    let serialise (w: World): byte[] =
        let bounds =
            ListSrl.serialise
                RectSrl.serialise
                (Set.toList w.bounds)

        let spawner =
            PointSrl.serialise w.playerSpawner

        let objects =
            ListSrl.serialise
                WorldObjectSrl.serialise
                (World.objectList w)

        let interactions =
            MapSrl.serialise
                StringSrl.serialise
                StringSrl.serialise
                w.interactions

        Array.concat [bounds; spawner; objects; interactions]

    let deserialise (bytes: byte[]): World DesrlResult =
        let getBounds = ListSrl.deserialise RectSrl.deserialise
        let getSpawner = PointSrl.deserialise
        let getObjects = ListSrl.deserialise WorldObjectSrl.deserialise
        let getInteractions = MapSrl.deserialise StringSrl.deserialise StringSrl.deserialise

        DesrlUtil.getFour
            getBounds
            getSpawner
            getObjects
            getInteractions
            World.createWithInteractions
            bytes
