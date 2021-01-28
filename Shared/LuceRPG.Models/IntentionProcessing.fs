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
                let tBusyUntil =
                    objectBusyMap
                    |> Map.tryFind id

                let objectBusy =
                    tBusyUntil
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
                        let travelTime = WorldObject.travelTime obj.value
                        if travelTime <= 0L
                        then thisUnchanged
                        else
                            let newObj = WorldObject.moveObject dir obj

                            if not (World.canPlace newObj world)
                            then thisUnchanged
                            else
                                let newWorld = World.addObject newObj world
                                let event =
                                    WorldEvent.Type.Moved (id, dir)
                                    |> WorldEvent.asResult intention.id

                                let movementStart =
                                    tBusyUntil
                                    |> Option.map (fun busyUntil ->
                                        max tsIntention.timestamp busyUntil
                                    )
                                    |> Option.defaultValue tsIntention.timestamp

                                let movementEnd = movementStart + travelTime

                                let newObjectBusyMap =
                                    objectBusyMap
                                    |> Map.add id movementEnd

                                let delayed =
                                    if amount = 1uy
                                    then []
                                    else
                                        let intention =
                                            Intention.Move (id, dir, amount - 1uy)
                                            |> Intention.makePayload intention.value.clientId
                                            |> WithId.useId intention.id
                                            |> WithTimestamp.create tsIntention.timestamp
                                        [intention]

                                {
                                    events = [event]
                                    delayed = delayed
                                    world = newWorld
                                    objectClientMap = objectClientMap
                                    objectBusyMap = newObjectBusyMap
                                }

        | Intention.JoinGame username ->
            // Generates event to add a player object to the world at the spawn point
            let spawnPoint = World.spawnPoint world
            let playerData = PlayerData.create username
            let obj =
                WorldObject.create
                    (WorldObject.Type.Player playerData)
                    spawnPoint
                |> WithId.create

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

            let updatedBusyMap =
                objectBusyMap
                |> Map.filter (fun id until -> not(List.contains id clientObjectsList))

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
                objectBusyMap = updatedBusyMap
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
