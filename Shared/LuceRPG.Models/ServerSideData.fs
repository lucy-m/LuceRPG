namespace LuceRPG.Models

module ServerSideData =
    type ObjectClientMap = Map<Id.WorldObject, Id.Client>
    type UsernameClientMap = Map<string, Id.Client>
    type ClientWorldMap = Map<Id.Client, Id.World>

    type WorldObjectClientMap = Map<Id.World, ObjectClientMap>

    type Model =
        {
            worldObjectClientMap: WorldObjectClientMap
            usernameClientMap: UsernameClientMap
            clientWorldMap: ClientWorldMap
            defaultWorld: Id.World
        }

    let create
            (worldObjectClientMap: WorldObjectClientMap)
            (usernameClientMap: UsernameClientMap)
            (clientWorldMap: ClientWorldMap)
            (defaultWorld: Id.World)
            : Model =
        {
            worldObjectClientMap = worldObjectClientMap
            usernameClientMap = usernameClientMap
            clientWorldMap = clientWorldMap
            defaultWorld = defaultWorld
        }

    let empty (defaultWorld: Id.World): Model =
        {
            worldObjectClientMap = Map.empty
            usernameClientMap = Map.empty
            clientWorldMap = Map.empty
            defaultWorld = defaultWorld
        }

    let addToWocm
            (worldId: Id.World)
            (objectId: Id.WorldObject)
            (clientId: Id.Client)
            (model: WorldObjectClientMap)
            : WorldObjectClientMap =
        let ocm =
            model
            |> Map.tryFind worldId
            |> Option.defaultValue Map.empty
            |> Map.add objectId clientId

        model
        |> Map.add worldId ocm

type ServerSideData = ServerSideData.Model
