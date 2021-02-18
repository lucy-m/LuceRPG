namespace LuceRPG.Models

module IntentionProcessing =
    type ObjectBusyMap = Map<Id.WorldObject, int64>

    type ProcessWorldResult =
        {
            events: WorldEvent seq
            delayed: IndexedIntention seq
            world: World
            objectBusyMap: ObjectBusyMap
        }

    let unchangedWorld
            (objectBusyMap: ObjectBusyMap)
            (world: World)
            : ProcessWorldResult =
        {
            events = []
            delayed = []
            world = world
            objectBusyMap = objectBusyMap
        }

    let processWorld
            (now: int64)
            (tObjectClientMap: ServerSideData.ObjectClientMap Option)
            (objectBusyMap: ObjectBusyMap)
            (world: World)
            (iIntention: IndexedIntention)
            : ProcessWorldResult =

        let thisUnchanged = unchangedWorld objectBusyMap world
        let intention = iIntention.tsIntention.value
        let timestamp = iIntention.tsIntention.timestamp
        let clientId = intention.value.clientId

        match intention.value.t with
        | Intention.Move (id, dir, amount) ->
            // May generate an event to move the object to its target location
            let clientOwnsObject =
                tObjectClientMap
                |> Option.map (fun objectClientMap ->
                    objectClientMap
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
                                            |> IndexedIntention.useIndex (iIntention.index + 1) (world.id)
                                        [intention]

                                {
                                    events = [event]
                                    delayed = delayed
                                    world = newWorld
                                    objectBusyMap = newObjectBusyMap
                                }

        // These are not processed at the world level
        | Intention.JoinGame _ -> thisUnchanged
        | Intention.LeaveGame _ -> thisUnchanged

    type ProcessGlobalResult =
        {
            events: WorldEvent seq
            delayed: IndexedIntention seq
            worldMap: World.Map
            objectBusyMap: ObjectBusyMap
            serverSideData: ServerSideData
        }

    let unchangedGlobal
            (worlds: World.Map)
            (objectBusyMap: ObjectBusyMap)
            (serverSideData: ServerSideData)
            : ProcessGlobalResult =
        {
            events = []
            delayed = []
            worldMap = worlds
            objectBusyMap = objectBusyMap
            serverSideData = serverSideData
        }

    let processGlobal
            (serverSideData: ServerSideData)
            (objectBusyMap: ObjectBusyMap)
            (worldMap: World.Map)
            (iIntention: IndexedIntention)
            : ProcessGlobalResult =

        let thisUnchanged =  unchangedGlobal worldMap objectBusyMap serverSideData
        let intention = iIntention.tsIntention.value
        let timestamp = iIntention.tsIntention.timestamp
        let clientId = intention.value.clientId
        let worldId = serverSideData.defaultWorld

        match intention.value.t with
        | Intention.JoinGame username ->
            let tWorld = worldMap |> Map.tryFind worldId
            match tWorld with
            | Option.None -> thisUnchanged
            | Option.Some world ->

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
                        serverSideData.usernameClientMap
                        |> Map.tryFind username

                    existingClientId
                    |> Option.map(fun id ->
                        let intention =
                            Intention.LeaveGame
                            |> Intention.makePayload id
                            |> WithId.create
                            |> WithTimestamp.create timestamp
                            |> IndexedIntention.create ""

                        [ intention ]
                    )
                    |> Option.defaultValue []

                let newServerSideData =
                    let objectClientMap =
                        serverSideData.worldObjectClientMap
                        |> Map.tryFind worldId
                        |> Option.defaultValue Map.empty
                        |> Map.add obj.id clientId

                    let worldObjectClientMap =
                        serverSideData.worldObjectClientMap
                        |> Map.add worldId objectClientMap

                    let usernameClientMap =
                        serverSideData.usernameClientMap
                        |> Map.add username clientId

                    let clientWorldMap =
                        serverSideData.clientWorldMap
                        |> Map.add clientId world.id

                    let defaultWorld = serverSideData.defaultWorld

                    ServerSideData.create
                        worldObjectClientMap
                        usernameClientMap
                        clientWorldMap
                        defaultWorld

                let event =
                    WorldEvent.Type.ObjectAdded obj
                    |> WorldEvent.asResult intention.id world.id iIntention.index

                let newWorld = EventApply.apply event world
                let newWorldMap = worldMap |> Map.add worldId newWorld

                {
                    events = [event]
                    delayed = removeExisting
                    worldMap = newWorldMap
                    objectBusyMap = objectBusyMap
                    serverSideData = newServerSideData
                }

        | Intention.LeaveGame ->
            let updatedServerSideData, updatedBusyMap, removeEvents =
                // For each world,
                //   update the objectClientMap
                //   generate a list of object ids to remove

                let wocm, toRemove =
                    serverSideData.worldObjectClientMap
                    |> Map.toSeq
                    |> Seq.map (fun (worldId, ocm) ->
                        let removeObjects, keepObjects =
                            ocm |> Map.partition (fun oId cId -> cId = clientId)

                        let updatedObjectClientMap = keepObjects
                        let toRemove =
                            removeObjects
                            |>
                            Map.toSeq
                            |> Seq.map fst
                            |> Seq.map (fun oId -> worldId, oId)

                        worldId, updatedObjectClientMap, toRemove
                    )
                    |> Seq.fold (fun acc (worldId, ocm, toRemove) ->
                        let wocm = fst acc |> Map.add worldId ocm
                        let allToRemove = snd acc |> Seq.append toRemove

                        wocm, allToRemove
                    ) (serverSideData.worldObjectClientMap, Seq.empty<Id.World * Id.WorldObject>)
                    |> fun (wocm, toRemove) ->
                        wocm, Set.ofSeq toRemove

                // Create events to remove all objects
                let removeEvents =
                    toRemove
                    |> Set.map (fun (worldId, oId) ->
                        WorldEvent.Type.ObjectRemoved oId
                        |> WorldEvent.asResult intention.id worldId iIntention.index
                    )
                    |> Set.toSeq

                // Update busy map to remove objects
                let updatedBusyMap =
                    let objectIds = toRemove |> Set.map snd

                    objectBusyMap
                    |> Map.filter (fun oId until -> not(Set.contains oId objectIds))

                let updatedServerSideData =
                    let usernameClientMap =
                        serverSideData.usernameClientMap
                        |> Map.filter(fun u cId -> cId <> clientId)

                    let clientWorldMap =
                        serverSideData.clientWorldMap
                        |> Map.remove clientId

                    let defaultWorld = serverSideData.defaultWorld

                    ServerSideData.create
                        wocm
                        usernameClientMap
                        clientWorldMap
                        defaultWorld

                updatedServerSideData, updatedBusyMap, removeEvents

            let newWorldMap =
                removeEvents
                |> Seq.fold (fun acc ev ->
                    let tWorld = acc |> Map.tryFind ev.world
                    let tUpdatedWorld = tWorld |> Option.map (EventApply.apply ev)

                    tUpdatedWorld
                    |> Option.map (fun uw -> acc |> Map.add ev.world uw)
                    |> Option.defaultValue acc
                ) worldMap

            {
                events = removeEvents
                delayed = []
                worldMap = newWorldMap
                objectBusyMap = updatedBusyMap
                serverSideData = updatedServerSideData
            }
        | Intention.Move _ -> thisUnchanged

    type ProcessManyResult =
        {
            events: WorldEvent seq
            delayed: IndexedIntention seq
            worldMap: Map<Id.World, World>
            objectBusyMap: ObjectBusyMap
            serverSideData: ServerSideData
        }

    let unchanged
            (serverSideData: ServerSideData)
            (objectBusyMap: ObjectBusyMap)
            (worldMap: Map<Id.World, World>)
            : ProcessManyResult =
        {
            events = []
            delayed = []
            worldMap = worldMap
            objectBusyMap = objectBusyMap
            serverSideData = serverSideData
        }

    /// Processes many intentions sequentially
    /// Will ensure timestamps are processed in timestamp order
    let processMany
            (now: int64)
            (serverSideData: ServerSideData)
            (objectBusyMap: ObjectBusyMap)
            (worlds: Map<Id.World, World>)
            (intentions: IndexedIntention seq)
            : ProcessManyResult =

        let initial = unchanged serverSideData objectBusyMap worlds

        intentions
        |> Seq.sortBy (fun i -> i.tsIntention.timestamp)
        |> Seq.fold (fun acc i ->
            let resGlobal =
                processGlobal acc.serverSideData acc.objectBusyMap acc.worldMap i

            let tWorld = resGlobal.worldMap |> Map.tryFind i.worldId

            match tWorld with
            | Option.None ->
                printf "Intention received for unknown world id %s" i.worldId

                {
                    events = Seq.append acc.events resGlobal.events
                    delayed = Seq.append acc.delayed resGlobal.delayed
                    worldMap = resGlobal.worldMap
                    objectBusyMap = resGlobal.objectBusyMap
                    serverSideData = resGlobal.serverSideData
                }
            | Option.Some world ->
                let tObjectClientMap =
                    resGlobal.serverSideData.worldObjectClientMap
                    |> Map.tryFind i.worldId

                let resWorld =
                    processWorld
                        now
                        tObjectClientMap
                        resGlobal.objectBusyMap
                        world
                        i

                let worldMap =
                    resGlobal.worldMap
                    |> Map.add i.worldId resWorld.world

                {
                    events = Seq.concat [acc.events; resGlobal.events; resWorld.events]
                    delayed = Seq.concat [acc.delayed; resGlobal.delayed; resWorld.delayed]
                    worldMap = worldMap
                    objectBusyMap = resWorld.objectBusyMap
                    serverSideData = resGlobal.serverSideData
                }
        ) initial
