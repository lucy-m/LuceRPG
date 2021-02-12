namespace LuceRPG.Models

module ServerSideData =
    type ObjectClientMap = Map<Id.WorldObject, Id.Client>
    type UsernameClientMap = Map<string, Id.Client>

    type Model =
        {
            objectClientMap: ObjectClientMap
            usernameClientMap: UsernameClientMap
        }

    let create
            (objectClientMap: ObjectClientMap)
            (usernameClientMap: UsernameClientMap)
            : Model =
        {
            objectClientMap = objectClientMap
            usernameClientMap = usernameClientMap
        }

    let empty: Model =
        {
            objectClientMap = Map.empty
            usernameClientMap = Map.empty
        }

type ServerSideData = ServerSideData.Model
