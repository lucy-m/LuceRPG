namespace LuceRPG.Models

module IntentionProcessing =
    type ClientObjectMap = Map<Id.WorldObject, Id.Client>

    type ProcessResult =
        {
            events: WorldEvent seq
            world: World
            clientObjectMap: ClientObjectMap
        }

    let unchanged (clientObjectMap: ClientObjectMap) (world: World): ProcessResult =
        {
            events = []
            world = world
            clientObjectMap = clientObjectMap
        }

    let processOne
            (clientObjectMap: ClientObjectMap)
            (world: World)
            (intention: Intention)
            : ProcessResult =

        let thisUnchanged = unchanged clientObjectMap world

        match intention.value.t with
        | Intention.Move (id, dir, amount) ->
            let clientOwnsObject =
                clientObjectMap
                |> Map.tryFind id
                |> Option.map (fun clientId -> clientId = intention.value.clientId)
                |> Option.defaultValue false

            if clientOwnsObject
            then
                let tObj = world.objects |> Map.tryFind id

                match tObj with
                | Option.None -> thisUnchanged
                | Option.Some obj ->
                    // Teleport to final location if clear
                    // Will do collision checking at a later date
                    let newObj = WorldObject.moveObject dir (int amount) obj

                    if World.canPlace newObj world
                    then
                        let newWorld = World.addObject newObj world
                        let event =
                            WorldEvent.Type.Moved (id, dir, amount)
                            |> WorldEvent.asResult intention.id

                        {
                            events = [event]
                            world = newWorld
                            clientObjectMap = clientObjectMap
                        }
                    else thisUnchanged
                else
                    thisUnchanged

        | Intention.JoinGame ->
            let spawnPoint = World.spawnPoint world
            let obj = WorldObject.create WorldObject.Type.Player spawnPoint |> WithId.create

            let newClientObjectMap =
                clientObjectMap
                |> Map.add intention.value.clientId obj.id

            let newWorld = World.addObject obj world
            let event =
                WorldEvent.Type.ObjectAdded obj
                |> WorldEvent.asResult intention.id

            {
                events = [event]
                world = newWorld
                clientObjectMap = newClientObjectMap
            }

    let processMany
            (clientObjectMap: Map<Id.WorldObject, Id.Client>)
            (world: World)
            (intentions: Intention seq)
            : ProcessResult =
        let initial = unchanged clientObjectMap world

        intentions
        |> Seq.fold (fun acc i ->
            let resultOne = processOne acc.clientObjectMap acc.world i

            {
                events = Seq.append resultOne.events acc.events
                world = resultOne.world
                clientObjectMap = resultOne.clientObjectMap
            }
        ) initial
