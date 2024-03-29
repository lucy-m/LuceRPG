﻿namespace LuceRPG.Models

module IntentionProcessing =

    type ProcessWorldResult =
        {
            events: WorldEvent seq
            delayed: IndexedIntention seq
            world: World
            objectBusyMap: ObjectBusyMap
            log: string Option
        }

    let unchangedWorld
            (objectBusyMap: ObjectBusyMap)
            (world: World)
            (log: string Option)
            : ProcessWorldResult =
        {
            events = []
            delayed = []
            world = world
            objectBusyMap = objectBusyMap
            log = log
        }

    let processWorld
            (now: int64)
            (tObjectClientMap: ServerSideData.ObjectClientMap Option)
            (tServerId: Id.Client Option)
            (objectBusyMap: ObjectBusyMap)
            (world: World)
            (iIntention: IndexedIntention)
            : ProcessWorldResult =

        let thisIgnored = unchangedWorld objectBusyMap world Option.None
        let thisUnchanged (log: string) = unchangedWorld objectBusyMap world (Option.Some log)
        let intention = iIntention.tsIntention.value
        let timestamp = iIntention.tsIntention.timestamp
        let clientId = intention.value.clientId

        let clientOwnsObject (id: Id.WorldObject): bool =
            let isServer =
                tServerId
                |> Option.map (fun serverId -> clientId = serverId)
                |> Option.defaultValue false

            if isServer
            then true
            else
                tObjectClientMap
                |> Option.map (fun objectClientMap ->
                    objectClientMap
                    |> Map.tryFind id
                    |> Option.map (fun cId -> cId = clientId)
                    |> Option.defaultValue false
                )
                |> Option.defaultValue true

        match intention.value.t with
        | Intention.Move (id, dir, amount) ->
            // May generate an event to move the object to its target location
            if not (clientOwnsObject id)
            then thisUnchanged (sprintf "Client %s does not own object %s" clientId id)
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
                        objectBusyMap = objectBusyMap
                        world = world
                        log = Option.None
                    }
                else
                    let tObj = world.value.objects |> Map.tryFind id

                    match tObj with
                    | Option.None -> thisUnchanged (sprintf "Unknown object %s in world %s" id world.id)
                    | Option.Some obj ->
                        let travelTime = WorldObject.travelTime obj.value
                        if travelTime <= 0L
                        then thisUnchanged (sprintf "Object %s cannot move" id)
                        else
                            let newObj = WithId.map (WorldObject.moveObject dir) obj

                            if not (World.canPlace newObj world.value)
                            then thisIgnored
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
                                    // Check if the object is now on a warp
                                    let tWarp = World.getWarp id newWorld.value
                                    match tWarp with
                                    | Option.Some warpData ->
                                        let intention =
                                            Intention.Warp (warpData, id)
                                            |> Intention.makePayload clientId
                                            |> WithId.useId intention.id
                                            |> WithTimestamp.create timestamp
                                            |> IndexedIntention.useIndex (iIntention.index + 1) (world.id)
                                        [intention]

                                    // Otherwise continue moving
                                    | Option.None ->
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
                                    objectBusyMap = newObjectBusyMap
                                    world = newWorld
                                    log = Option.None
                                }

        | Intention.TurnTowards (id, dir) ->
            if not (clientOwnsObject id)
            then thisUnchanged (sprintf "Client %s does not own object %s" clientId id)
            else
                let tObj = world.value.objects |> Map.tryFind id

                match tObj with
                | Option.None -> thisUnchanged (sprintf "Unknown object %s in world %s" id world.id)
                | Option.Some obj ->
                    let newObj = WithId.map (WorldObject.turnTowards dir) obj

                    if not (World.canPlace newObj world.value)
                    then thisIgnored
                    else
                        let event =
                            WorldEvent.TurnedTowards (id, dir)
                            |> WorldEvent.asResult intention.id world.id iIntention.index

                        let newWorld = EventApply.apply event world

                        {
                            events = [event]
                            delayed = []
                            objectBusyMap = objectBusyMap
                            world = newWorld
                            log = Option.None
                        }

        // These are not processed at the world level
        | Intention.JoinWorld _ -> thisIgnored
        | Intention.JoinGame _ -> thisIgnored
        | Intention.LeaveGame _ -> thisIgnored
        | Intention.LeaveWorld _ -> thisIgnored
        | Intention.Warp _ -> thisIgnored

    type ProcessGlobalResult =
        {
            events: WorldEvent seq
            delayed: IndexedIntention seq
            worldMap: World.Map
            objectBusyMap: ObjectBusyMap
            serverSideData: ServerSideData
            log: string Option
        }

    let unchangedGlobal
            (worlds: World.Map)
            (objectBusyMap: ObjectBusyMap)
            (serverSideData: ServerSideData)
            (log: string Option)
            : ProcessGlobalResult =
        {
            events = []
            delayed = []
            worldMap = worlds
            objectBusyMap = objectBusyMap
            serverSideData = serverSideData
            log = log
        }

    let processGlobal
            (serverSideData: ServerSideData)
            (objectBusyMap: ObjectBusyMap)
            (worldMap: World.Map)
            (iIntention: IndexedIntention)
            : ProcessGlobalResult =

        let thisUnchanged (log: string) = unchangedGlobal worldMap objectBusyMap serverSideData (Option.Some log)
        let thisIgnored = unchangedGlobal worldMap objectBusyMap serverSideData Option.None
        let intention = iIntention.tsIntention.value
        let timestamp = iIntention.tsIntention.timestamp
        let clientId = intention.value.clientId

        match intention.value.t with
        | Intention.JoinGame username ->
            let worldId = serverSideData.defaultWorld
            let tWorld = worldMap |> Map.tryFind worldId
            match tWorld with
            | Option.None -> thisUnchanged (sprintf "Default world id %s is invalid" worldId)
            | Option.Some world ->

                // Generates event to add a player object to the world at the spawn point
                let spawnPoint = World.spawnPoint world.value
                let charData = CharacterData.randomized username
                let obj =
                    WorldObject.create
                        (WorldObject.Type.Player charData)
                        spawnPoint
                        Direction.South
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
                    let worldObjectClientMap =
                        ServerSideData.addToWocm
                            worldId obj.id clientId
                            serverSideData.worldObjectClientMap

                    let usernameClientMap =
                        serverSideData.usernameClientMap
                        |> Map.add username clientId

                    let clientWorldMap =
                        serverSideData.clientWorldMap
                        |> Map.add clientId world.id

                    let generatedWorldMap = serverSideData.generatedWorldMap
                    let defaultWorld = serverSideData.defaultWorld
                    let serverId = serverSideData.serverId

                    ServerSideData.create
                        worldObjectClientMap
                        usernameClientMap
                        clientWorldMap
                        generatedWorldMap
                        defaultWorld
                        serverId

                let event =
                    WorldEvent.Type.ObjectAdded obj
                    |> WorldEvent.asResult intention.id worldId iIntention.index

                let newWorld = EventApply.apply event world
                let newWorldMap = worldMap |> Map.add worldId newWorld

                let joinedWorldEvent =
                    WorldEvent.JoinedWorld clientId
                    |> WorldEvent.asResult intention.id worldId (iIntention.index + 1)

                {
                    events = [event; joinedWorldEvent]
                    delayed = removeExisting
                    worldMap = newWorldMap
                    objectBusyMap = objectBusyMap
                    serverSideData = newServerSideData
                    log = Option.None
                }

        | Intention.LeaveGame ->
            // Removes all refs to the client from all world
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
                    toRemove
                    |> Set.fold (fun acc (wId, oId) ->
                        acc |> Map.remove oId
                    ) objectBusyMap

                let updatedServerSideData =
                    let usernameClientMap =
                        serverSideData.usernameClientMap
                        |> Map.filter(fun u cId -> cId <> clientId)

                    let clientWorldMap =
                        serverSideData.clientWorldMap
                        |> Map.remove clientId

                    let generatedWorldMap = serverSideData.generatedWorldMap
                    let defaultWorld = serverSideData.defaultWorld
                    let serverId = serverSideData.serverId

                    ServerSideData.create
                        wocm
                        usernameClientMap
                        clientWorldMap
                        generatedWorldMap
                        defaultWorld
                        serverId

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
                log = Option.None
            }

        | Intention.JoinWorld obj ->
            let worldId = iIntention.worldId
            let tWorld = worldMap |> Map.tryFind worldId

            match tWorld with
            | Option.None -> thisUnchanged (sprintf "Cannot join unknown world %s" worldId)
            | Option.Some world ->
                // Try to add the object to the world
                let tEventType =
                    if World.canPlace obj world.value
                    then WorldEvent.Type.ObjectAdded obj |> Option.Some
                    else
                        let atSpawnPoint = WithId.map (WorldObject.atLocation world.value.playerSpawner) obj
                        if World.canPlace atSpawnPoint world.value
                        then WorldEvent.Type.ObjectAdded atSpawnPoint |> Option.Some
                        else Option.None

                match tEventType with
                | Option.None -> thisUnchanged (sprintf "No valid location to place object in world %s" worldId)
                | Option.Some eventType ->
                    // apply the event to the world
                    let event = WorldEvent.asResult intention.id worldId iIntention.index eventType
                    let newWorld = EventApply.apply event world
                    let newWorldMap = worldMap |> Map.add worldId newWorld

                    // Add the object to the objectClientMap
                    let wocm =
                        ServerSideData.addToWocm
                            worldId
                            obj.id
                            clientId
                            serverSideData.worldObjectClientMap

                    // Change the clientWorldMap to be the new world
                    let cwm = serverSideData.clientWorldMap |> Map.add clientId worldId

                    let updatedServerSideData =
                        {
                            serverSideData with
                                worldObjectClientMap = wocm
                                clientWorldMap = cwm
                        }

                    let joinedWorldEvent =
                        WorldEvent.JoinedWorld clientId
                        |> WorldEvent.asResult intention.id worldId (iIntention.index + 1)

                    {
                        events = [event; joinedWorldEvent]
                        delayed = []
                        worldMap = newWorldMap
                        objectBusyMap = objectBusyMap
                        serverSideData = updatedServerSideData
                        log = Option.None
                    }

        | Intention.LeaveWorld ->
            // Removes client data from the specified world
            // Objects
            // Object busy map
            // ObjectClientMap
            let worldId = iIntention.worldId
            let tWorld = worldMap |> Map.tryFind worldId

            match tWorld with
            | Option.None -> thisUnchanged (sprintf "Cannot leave unknown world %s" worldId)
            | Option.Some world ->
                let toRemove, newOcm =
                    serverSideData.worldObjectClientMap
                    |> Map.tryFind worldId
                    |> Option.defaultValue Map.empty
                    |> Map.partition (fun oId cId ->
                        cId = clientId
                    )
                    |> fun (toRemove, newOcm) ->
                        toRemove |> Map.toList |> List.map fst, newOcm

                let removeEvents =
                    toRemove
                    |> List.map (fun oId ->
                        WorldEvent.Type.ObjectRemoved oId
                        |> WorldEvent.asResult intention.id worldId iIntention.index
                    )

                let newWorldMap =
                    let newWorld = EventApply.applyMany removeEvents world
                    worldMap |> Map.add worldId newWorld

                let updatedBusyMap =
                    toRemove
                    |> List.fold (fun acc oId ->
                        acc |> Map.remove oId
                    ) objectBusyMap

                let updatedServerSideData =
                    let worldObjectClientMap =
                        serverSideData.worldObjectClientMap
                        |> Map.add worldId newOcm

                    let clientWorldMap =
                        // Remove the client -> world entry if it
                        //   is this world
                        let tCurrentWorld =
                            serverSideData.clientWorldMap
                            |> Map.tryFind clientId

                        match tCurrentWorld with
                        | Option.None -> serverSideData.clientWorldMap
                        | Option.Some currentWorld ->
                            if currentWorld = worldId
                            then serverSideData.clientWorldMap |> Map.remove clientId
                            else serverSideData.clientWorldMap

                    {
                        serverSideData with
                            worldObjectClientMap = worldObjectClientMap
                            clientWorldMap = clientWorldMap
                    }

                {
                    events = removeEvents
                    delayed = []
                    worldMap = newWorldMap
                    objectBusyMap = updatedBusyMap
                    serverSideData = updatedServerSideData
                    log = Option.None
                }

        | Intention.Warp (warpData, objectId) ->
            match warpData with
            | Warp.Static (toWorldId, toPoint) ->
                // If world ID exists and object ID points to a valid
                //   object in the current world then create JoinWorld
                //   and LeaveWorld intentions
                let fromWorldId = iIntention.worldId
                let tToWorld = worldMap |> Map.tryFind toWorldId
                let tFromWorld = worldMap |> Map.tryFind fromWorldId
                let tObject =
                    tFromWorld
                    |> Option.bind (fun fromWorld ->
                        fromWorld.value.objects
                        |> Map.tryFind objectId
                    )
                    |> Option.map (fun oldPosition ->
                        WithId.map (WorldObject.atLocation toPoint) oldPosition
                    )

                match (tToWorld, tObject) with
                | Option.Some toWorld, Option.Some obj ->
                    let joinWorldIntention =
                        Intention.JoinWorld obj
                        |> Intention.makePayload clientId
                        |> WithId.create
                        |> WithTimestamp.create timestamp
                        |> IndexedIntention.useIndex (iIntention.index + 1) toWorld.id

                    let leaveWorldIntention =
                        Intention.LeaveWorld
                        |> Intention.makePayload clientId
                        |> WithId.create
                        |> WithTimestamp.create timestamp
                        |> IndexedIntention.useIndex (iIntention.index + 1) fromWorldId

                    let delayed = [joinWorldIntention; leaveWorldIntention]

                    {
                        events = []
                        delayed = delayed
                        worldMap = worldMap
                        objectBusyMap = objectBusyMap
                        serverSideData = serverSideData
                        log = Option.None
                    }
                | _ -> thisUnchanged (sprintf "Unknown world %s or object %s" fromWorldId objectId)

            | Warp.Dynamic (toSeed, outDirection, index) ->
                serverSideData.generatedWorldMap
                |> Map.tryFind toSeed
                |> Option.map (fun worldId ->
                    worldMap
                    |> Map.tryFind worldId
                    |> Option.map (fun toWorld ->
                        // This would has already been generated, convert
                        //   to a static warp
                        let inDirection = Direction.inverse outDirection

                        // Find all valid target points
                        let targetPoints =
                            toWorld.value.dynamicWarps
                            |> Map.toSeq
                            |> Seq.filter (fun (p,d) -> d = inDirection)
                            |> Seq.map (fun (p,d) ->
                                Direction.movePoint outDirection 2 p
                            )

                        // Index by position along x or y axis
                        let indexed =
                            targetPoints
                            |> Seq.sortBy (fun p ->
                                match outDirection with
                                | Direction.North | Direction.South -> p.x
                                | Direction.East | Direction.West -> p.y
                            )
                            |> Seq.indexed

                        // Find point with matching index, or first, or the spawn
                        let point =
                            indexed
                            |> Seq.tryFind (fun (i, p) -> i = index)
                            |> Option.map snd
                            |> Option.defaultWith (fun () ->
                                indexed
                                |> Seq.tryHead
                                |> Option.map snd
                                |> Option.defaultValue toWorld.value.playerSpawner
                            )

                        let staticWarp = Warp.createTarget worldId point

                        let warpIntention =
                            Intention.Warp (staticWarp, objectId)
                            |> Intention.makePayload clientId
                            |> WithId.create
                            |> WithTimestamp.create timestamp
                            |> IndexedIntention.useIndex (iIntention.index + 1) iIntention.worldId

                        let log =
                            sprintf "Converted dynamic warp %i index %i to static warp %s (%i, %i)"
                                toSeed
                                index
                                worldId
                                point.x
                                point.y
                            |> Option.Some

                        {
                            events = []
                            delayed = [warpIntention]
                            worldMap = worldMap
                            objectBusyMap = objectBusyMap
                            serverSideData = serverSideData
                            log = log
                        }
                    )
                    |> Option.defaultWith(fun () ->
                        // The generated world map entry doesn't point to a valid
                        //   map, remove the entry and try again
                        let generatedWorldMap =
                            serverSideData.generatedWorldMap
                            |> Map.remove toSeed

                        let serverSideData =
                            {
                                serverSideData with generatedWorldMap = generatedWorldMap
                            }

                        let log =
                            sprintf "Invalid entry in generated world map %i - %s"
                                toSeed
                                worldId
                            |> Option.Some

                        {
                            events = []
                            delayed = [iIntention]
                            worldMap = worldMap
                            objectBusyMap = objectBusyMap
                            serverSideData = serverSideData
                            log = log
                        }
                    )
                )
                |> Option.defaultWith (fun () ->
                    // This world needs to be generated
                    let event =
                        WorldEvent.WorldGenerateRequest (toSeed, outDirection)
                        |> WorldEvent.asResult intention.id iIntention.worldId iIntention.index
                    let log =
                        sprintf "Requesting generation of %i in direction %c"
                            toSeed
                            (Direction.asLetter outDirection)
                        |> Option.Some

                    {
                        events = [event]
                        delayed = [iIntention]
                        worldMap = worldMap
                        objectBusyMap = objectBusyMap
                        serverSideData = serverSideData
                        log = log
                    }
                )

        | Intention.Move _ -> thisIgnored
        | Intention.TurnTowards _ -> thisIgnored

    type ProcessManyResult =
        {
            events: WorldEvent seq
            delayed: IndexedIntention seq
            worldMap: Map<Id.World, World>
            objectBusyMap: ObjectBusyMap
            serverSideData: ServerSideData
            logs: string seq
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
            logs = []
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
            let globalLog =
                match resGlobal.log with
                | Option.Some log -> Seq.singleton log
                | Option.None -> Seq.empty

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
                    logs = Seq.append acc.logs globalLog
                }
            | Option.Some world ->
                let tObjectClientMap =
                    resGlobal.serverSideData.worldObjectClientMap
                    |> Map.tryFind i.worldId

                let resWorld =
                    processWorld
                        now
                        tObjectClientMap
                        (Option.Some resGlobal.serverSideData.serverId)
                        resGlobal.objectBusyMap
                        world
                        i

                let worldLog =
                    match resWorld.log with
                    | Option.Some log -> Seq.singleton log
                    | Option.None -> Seq.empty

                let worldMap =
                    resGlobal.worldMap
                    |> Map.add i.worldId resWorld.world

                {
                    events = Seq.concat [acc.events; resGlobal.events; resWorld.events]
                    delayed = Seq.concat [acc.delayed; resGlobal.delayed; resWorld.delayed]
                    worldMap = worldMap
                    objectBusyMap = resWorld.objectBusyMap
                    serverSideData = resGlobal.serverSideData
                    logs = Seq.concat [acc.logs; globalLog; worldLog]
                }
        ) initial
