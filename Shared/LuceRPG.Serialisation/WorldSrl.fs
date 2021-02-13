namespace LuceRPG.Serialisation

open LuceRPG.Models

module WorldSrl =
    let serialisePayload (w: World.Payload): byte[] =
        let name =
            StringSrl.serialise w.name

        let bounds =
            ListSrl.serialise
                RectSrl.serialise
                (Set.toList w.bounds)

        let spawner =
            PointSrl.serialise w.playerSpawner

        let objects =
            ListSrl.serialise
                WorldObjectSrl.serialise
                (w.objects |> WithId.toList)

        let interactions =
            MapSrl.serialise
                StringSrl.serialise
                StringSrl.serialise
                w.interactions

        Array.concat [name; bounds; spawner; objects; interactions]

    let serialise (w: World): byte[] =
        WithIdSrl.serialise serialisePayload w

    let deserialisePayload (bytes: byte[]): World.Payload DesrlResult =
        let getName = StringSrl.deserialise
        let getBounds = ListSrl.deserialise RectSrl.deserialise
        let getSpawner = PointSrl.deserialise
        let getObjects = ListSrl.deserialise WorldObjectSrl.deserialise
        let getInteractions = MapSrl.deserialise StringSrl.deserialise StringSrl.deserialise

        DesrlUtil.getFive
            getName
            getBounds
            getSpawner
            getObjects
            getInteractions
            World.createWithInteractions
            bytes

    let deserialise (bytes: byte[]): World DesrlResult =
        WithIdSrl.deserialise deserialisePayload bytes
