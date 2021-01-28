namespace LuceRPG.Models

module IntentionProcessing =
    type ObjectClientMap = Map<Id.WorldObject, Id.Client>
    type ObjectBusyMap = Map<Id.WorldObject, int64>

    type ProcessResult =
        {
            events: WorldEvent seq
            delayed: Intention WithTimestamp seq
            world: World
            objectClientMap: ObjectClientMap
            objectBusyMap: ObjectBusyMap
        }

    let unchanged
            (objectClientMap: ObjectClientMap)
            (objectBusyMap: ObjectBusyMap)
            (world: World)
            : ProcessResult =
        {
            events = []
            delayed = []
            world = world
            objectClientMap = objectClientMap
            objectBusyMap = objectBusyMap
        }

    let processOne
            (now: int64)
            (objectClientMap: ObjectClientMap)
            (objectBusyMap: ObjectBusyMap)
            (world: World)
            (tsIntention: Intention WithTimestamp)
            : ProcessResult =

        let thisUnchanged = unchanged objectClientMap objectBusyMap world
        let intention = tsIntention.value

        match intention.value.t with
        | Intention.Move (id, dir, amount) ->
            // May generate an event to move the object to its target location
            let clientOwnsObject =
                objectClientMap
                |> Map.tryFind id
                |> Option.map (fun clientId -> clientId = intention.value.clientId)
                |> Option.defaultValue false

            if not clientOwnsObject
            then thisUnchanged
            else
                let objectBusy =
                    objectBusyMap
                    |> Map.tryFind id
                    |> Option.map (fun until -> until > now)
                    |> Option.defaultValue false

                if objectBusy
                then
                    {
                        events = []
                        delayed = [tsIntention]
                        world = world
                        objectClientMap = objectClientMap
                        objectBusyMap = objectBusyMap
                    }
                else
                    let tObj = world.objects |> Map.tryFind id

                    match tObj with
                    | Option.None -> thisUnchanged
                    | Option.Some obj ->
                        // Teleport to final location if clear
                        // Will do collision checking at a later date
                        let newObj = WorldObject.moveObject dir (int amount) obj

                        if not (World.canPlace newObj world)
                        then thisUnchanged
                        else
                            let newWorld = World.addObject newObj world
                            let event =
                                WorldEvent.Type.Moved (id, dir, amount)
                                |> WorldEvent.asResult intention.id
                            let busyUntil = now + System.TimeSpan.FromMilliseconds(float(100)).Ticks
                            let newObjectBusyMap =
                                objectBusyMap
                                |> Map.add id busyUntil

                            {
                                events = [event]
                                delayed = []
                                world = newWorld
                                objectClientMap = objectClientMap
                                objectBusyMap = newObjectBusyMap
                            }

        | Intention.JoinGame ->
            // Generates event to add a player object to the world at the spawn point
            let spawnPoint = World.spawnPoint world
            let obj = WorldObject.create WorldObject.Type.Player spawnPoint |> WithId.create

            let newClientObjectMap =
                objectClientMap
                |> Map.add obj.id intention.value.clientId

            let newWorld = World.addObject obj world
            let event =
                WorldEvent.Type.ObjectAdded obj
                |> WorldEvent.asResult intention.id

            {
                events = [event]
                delayed = []
                world = newWorld
                objectClientMap = newClientObjectMap
                objectBusyMap = objectBusyMap
            }

        | Intention.LeaveGame ->
            // Generates events to remove all objects relating to the client ID
            let clientObjects, updatedObjectClientMap =
                objectClientMap
                |> Map.partition (fun oId cId -> cId = intention.value.clientId)

            let clientObjectsList =
                clientObjects
                |> Map.toList
                |> List.map fst

            let removeEvents =
                clientObjectsList
                |> List.map (fun e ->
                    WorldEvent.Type.ObjectRemoved e
                    |> WorldEvent.asResult intention.id
                )

            let updatedWorld =
                clientObjectsList
                |> List.fold (fun acc oId ->
                    World.removeObject oId acc
                ) world

            {
                events = removeEvents
                delayed = []
                world = updatedWorld
                objectClientMap = updatedObjectClientMap
                objectBusyMap = objectBusyMap
            }

    /// Processes many intentions sequentially
    /// Will ensure timestamps are processed in timestamp order
    let processMany
            (now: int64)
            (objectClientMap: ObjectClientMap)
            (objectBusyMap: ObjectBusyMap)
            (world: World)
            (intentions: Intention WithTimestamp seq)
            : ProcessResult =
        let initial = unchanged objectClientMap objectBusyMap world

        intentions
        |> Seq.sortBy (fun i -> i.timestamp)
        |> Seq.fold (fun acc i ->
            let resultOne = processOne now acc.objectClientMap acc.objectBusyMap acc.world i

            {
                events = Seq.append acc.events resultOne.events
                delayed = Seq.append acc.delayed resultOne.delayed
                world = resultOne.world
                objectClientMap = resultOne.objectClientMap
                objectBusyMap = resultOne.objectBusyMap
            }
        ) initial
