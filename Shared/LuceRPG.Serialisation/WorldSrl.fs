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

        Array.concat [bounds; spawner; objects]

    let deserialise (bytes: byte[]): World DesrlResult =
        let getBounds = ListSrl.deserialise RectSrl.deserialise
        let getSpawner = PointSrl.deserialise
        let getObjects = ListSrl.deserialise WorldObjectSrl.deserialise

        DesrlUtil.getThree
            getBounds
            getSpawner
            getObjects
            World.createWithObjs
            bytes
