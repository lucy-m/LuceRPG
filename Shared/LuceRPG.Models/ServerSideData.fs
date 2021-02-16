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
        }

    let create
            (objectClientMap: ObjectClientMap)
            (usernameClientMap: UsernameClientMap)
            (clientWorldMap: ClientWorldMap)
            : Model =
        {
            objectClientMap = objectClientMap
            usernameClientMap = usernameClientMap
            clientWorldMap = clientWorldMap
        }

    let empty: Model =
        {
            objectClientMap = Map.empty
            usernameClientMap = Map.empty
            clientWorldMap = Map.empty
        }

type ServerSideData = ServerSideData.Model
