namespace LuceRPG.Models

module IntentionProcessing =
    type ObjectBusyMap = Map<Id.WorldObject, int64>

    type ProcessResult =
        {
            events: WorldEvent seq
            delayed: IndexedIntention seq
            world: World
            objectBusyMap: ObjectBusyMap
            serverSideData: ServerSideData Option
        }

    let objectClientMap (pr: ProcessResult): ServerSideData.ObjectClientMap Option =
        pr.serverSideData |> Option.map (fun ssd -> ssd.objectClientMap)

    let usernameClientMap (pr: ProcessResult): ServerSideData.UsernameClientMap Option =
        pr.serverSideData |> Option.map (fun ssd -> ssd.usernameClientMap)

    let clientWorldMap (pr: ProcessResult): ServerSideData.ClientWorldMap Option =
        pr.serverSideData |> Option.map (fun ssd -> ssd.clientWorldMap)

    let unchanged
            (serverSideData: ServerSideData Option)
            (objectBusyMap: ObjectBusyMap)
            (world: World)
            : ProcessResult =
        {
            events = []
            delayed = []
            world = world
            objectBusyMap = objectBusyMap
            serverSideData = serverSideData
        }

    let processOne
            (now: int64)
            (tServerSideData: ServerSideData Option)
            (objectBusyMap: ObjectBusyMap)
            (world: World)
            (iIntention: IndexedIntention)
            : ProcessResult =

        let thisUnchanged = unchanged tServerSideData objectBusyMap world
        let intention = iIntention.tsIntention.value
        let timestamp = iIntention.tsIntention.timestamp
        let clientId = intention.value.clientId

        match intention.value.t with
        | Intention.Move (id, dir, amount) ->
            // May generate an event to move the object to its target location
            let clientOwnsObject =
                tServerSideData
                |> Option.map (fun serverSideData ->
                    serverSideData.objectClientMap
                    |> Map.tryFind id
                    |> Option.map (fun cId -> cId = clientId)
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
                        objectBusyMap = objectBusyMap
                        serverSideData = tServerSideData
                    }
                else
                    let tObj = world.value.objects |> Map.tryFind id

                    match tObj with
                    | Option.None -> thisUnchanged
                    | Option.Some obj ->
                        let travelTime = WorldObject.travelTime obj.value
                        if travelTime <= 0L
                        then thisUnchanged
                        else
                            let newObj = WithId.map (WorldObject.moveObject dir) obj

                            if not (World.canPlace newObj world.value)
                            then thisUnchanged
                            else
                                let event =
                                    WorldEvent.Type.Moved (id, dir)
                                    |> WorldEvent.asResult intention.id world.id iIntention.index

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
                                            |> Intention.makePayload clientId
                                            |> WithId.useId intention.id
                                            |> WithTimestamp.create timestamp
                                            |> IndexedIntention.useIndex (iIntention.index + 1)
                                        [intention]

                                {
                                    events = [event]
                                    delayed = delayed
                                    world = newWorld
                                    objectBusyMap = newObjectBusyMap
                                    serverSideData = tServerSideData
                                }

        | Intention.JoinGame username ->
            // Generates event to add a player object to the world at the spawn point
            let spawnPoint = World.spawnPoint world.value
            let playerData = PlayerData.create username
            let obj =
                WorldObject.create
                    (WorldObject.Type.Player playerData)
                    spawnPoint
                |> WithId.create

            let removeExisting =
                let existingClientId =
                    tServerSideData
                    |> Option.bind (fun ssd ->
                        ssd.usernameClientMap
                        |> Map.tryFind username
                    )

                existingClientId
                |> Option.map(fun id ->
                    let intention =
                        Intention.LeaveGame
                        |> Intention.makePayload id
                        |> WithId.create
                        |> WithTimestamp.create timestamp
                        |> IndexedIntention.create

                    [ intention ]
                )
                |> Option.defaultValue []

            let newServerSideData =
                tServerSideData
                |> Option.map (fun ssd ->
                    let objectClientMap =
                        ssd.objectClientMap
                        |> Map.add obj.id clientId

                    let usernameClientMap =
                        ssd.usernameClientMap
                        |> Map.add username clientId

                    let clientWorldMap =
                        ssd.clientWorldMap
                        |> Map.add clientId world.id

                    let defaultWorld = ssd.defaultWorld

                    ServerSideData.create
                        objectClientMap
                        usernameClientMap
                        clientWorldMap
                        defaultWorld
                )

            let event =
                WorldEvent.Type.ObjectAdded obj
                |> WorldEvent.asResult intention.id world.id iIntention.index

            let newWorld = EventApply.apply event world

            {
                events = [event]
                delayed = removeExisting
                world = newWorld
                objectBusyMap = objectBusyMap
                serverSideData = newServerSideData
            }

        | Intention.LeaveGame ->
            let updatedServerSideData, updatedBusyMap, removeEvents =
                tServerSideData
                |> Option.map (fun ssd ->
                    // Generates events to remove all objects relating to the client ID
                    let clientObjects, updatedObjectClientMap =
                        ssd.objectClientMap
                        |> Map.partition (fun oId cId -> cId = clientId)

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
                                |> WorldEvent.asResult intention.id world.id iIntention.index
                        )

                    let updatedServerSideData =
                        let usernameClientMap =
                            ssd.usernameClientMap
                            |> Map.filter(fun u cId -> cId <> clientId)

                        let clientWorldMap =
                            ssd.clientWorldMap
                            |> Map.remove clientId

                        let defaultWorld = ssd.defaultWorld

                        ServerSideData.create
                            updatedObjectClientMap
                            usernameClientMap
                            clientWorldMap
                            defaultWorld

                    Option.Some updatedServerSideData, updatedBusyMap, removeEvents
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
                objectBusyMap = updatedBusyMap
                serverSideData = updatedServerSideData
            }

    /// Processes many intentions sequentially
    /// Will ensure timestamps are processed in timestamp order
    let processMany
            (now: int64)
            (serverSideData: ServerSideData Option)
            (objectBusyMap: ObjectBusyMap)
            (world: World)
            (intentions: IndexedIntention seq)
            : ProcessResult =
        let initial = unchanged serverSideData objectBusyMap world

        intentions
        |> Seq.sortBy (fun i -> i.tsIntention.timestamp)
        |> Seq.fold (fun acc i ->
            let resultOne = processOne now acc.serverSideData acc.objectBusyMap acc.world i

            {
                events = Seq.append acc.events resultOne.events
                delayed = Seq.append acc.delayed resultOne.delayed
                world = resultOne.world
                objectBusyMap = resultOne.objectBusyMap
                serverSideData = resultOne.serverSideData
            }
        ) initial
