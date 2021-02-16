namespace LuceRPG.Models

module ServerSideData =
    type ObjectClientMap = Map<Id.WorldObject, Id.Client>
    type UsernameClientMap = Map<string, Id.Client>
    type ClientWorldMap = Map<Id.Client, Id.World>

    type Model =
        {
            objectClientMap: ObjectClientMap
            usernameClientMap: UsernameClientMap
            clientWorldMap: ClientWorldMap
            defaultWorld: Id.World
        }

    let create
            (objectClientMap: ObjectClientMap)
            (usernameClientMap: UsernameClientMap)
            (clientWorldMap: ClientWorldMap)
            (defaultWorld: Id.World)
            : Model =
        {
            objectClientMap = objectClientMap
            usernameClientMap = usernameClientMap
            clientWorldMap = clientWorldMap
            defaultWorld = defaultWorld
        }

    let empty (defaultWorld: Id.World): Model =
        {
            objectClientMap = Map.empty
            usernameClientMap = Map.empty
            clientWorldMap = Map.empty
            defaultWorld = defaultWorld
        }

type ServerSideData = ServerSideData.Model
