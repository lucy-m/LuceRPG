namespace LuceRPG.Models

module IntentionProcessing =
    type ObjectClientMap = Map<Id.WorldObject, Id.Client>
    type ObjectBusyMap = Map<Id.WorldObject, int64>

    type ProcessResult =
        {
            events: WorldEvent seq
            delayed: IndexedIntention seq
            world: World
            objectClientMap: ObjectClientMap Option
            objectBusyMap: ObjectBusyMap
        }

    let unchanged
            (objectClientMap: ObjectClientMap Option)
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
            (tObjectClientMap: ObjectClientMap Option)
            (objectBusyMap: ObjectBusyMap)
            (world: World)
            (iIntention: IndexedIntention)
            : ProcessResult =

        let thisUnchanged = unchanged tObjectClientMap objectBusyMap world
        let intention = iIntention.tsIntention.value
        let timestamp = iIntention.tsIntention.timestamp

        match intention.value.t with
        | Intention.Move (id, dir, amount) ->
            // May generate an event to move the object to its target location
            let clientOwnsObject =
                tObjectClientMap
                |> Option.map (fun objectClientMap ->
                    objectClientMap
                    |> Map.tryFind id
                    |> Option.map (fun clientId -> clientId = intention.value.clientId)
                    |> Option.defaultValue false
                )
                |> Option.defaultValue true

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
                        delayed = [iIntention]
                        world = world
                        objectClientMap = tObjectClientMap
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
                                let event =
                                    WorldEvent.Type.Moved (id, dir)
                                    |> WorldEvent.asResult intention.id iIntention.index

                                let newWorld = EventApply.apply event world

                                let movementStart =
                                    tBusyUntil
                                    |> Option.map (fun busyUntil ->
                                        max timestamp busyUntil
                                    )
                                    |> Option.defaultValue timestamp

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
                                            |> WithTimestamp.create timestamp
                                            |> IndexedIntention.useIndex (iIntention.index + 1)
                                        [intention]

                                {
                                    events = [event]
                                    delayed = delayed
                                    world = newWorld
                                    objectClientMap = tObjectClientMap
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
                tObjectClientMap
                |> Option.map (fun objectClientMap ->
                    objectClientMap
                    |> Map.add obj.id intention.value.clientId
                )

            let event =
                WorldEvent.Type.ObjectAdded obj
                |> WorldEvent.asResult intention.id iIntention.index

            let newWorld = EventApply.apply event world

            {
                events = [event]
                delayed = []
                world = newWorld
                objectClientMap = newClientObjectMap
                objectBusyMap = objectBusyMap
            }

        | Intention.LeaveGame ->
            let updatedObjectClientMap, updatedBusyMap, removeEvents =
                tObjectClientMap
                |> Option.map (fun objectClientMap ->
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
                                |> WorldEvent.asResult intention.id iIntention.index
                        )

                    Option.Some updatedObjectClientMap, updatedBusyMap, removeEvents
                )
                |> Option.defaultValue (Option.None, objectBusyMap, [])

            let updatedWorld =
                removeEvents
                |> List.fold (fun acc e ->
                    EventApply.apply e acc
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
            (objectClientMap: ObjectClientMap Option)
            (objectBusyMap: ObjectBusyMap)
            (world: World)
            (intentions: IndexedIntention seq)
            : ProcessResult =
        let initial = unchanged objectClientMap objectBusyMap world

        intentions
        |> Seq.sortBy (fun i -> i.tsIntention.timestamp)
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
