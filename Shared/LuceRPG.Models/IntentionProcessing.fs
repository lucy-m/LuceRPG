﻿namespace LuceRPG.Models

module IntentionProcessing =
    type ObjectClientMap = Map<Id.WorldObject, Id.Client>

    type ProcessResult =
        {
            events: WorldEvent seq
            world: World
            objectClientMap: ObjectClientMap
        }

    let unchanged (objectClientMap: ObjectClientMap) (world: World): ProcessResult =
        {
            events = []
            world = world
            objectClientMap = objectClientMap
        }

    let processOne
            (objectClientMap: ObjectClientMap)
            (world: World)
            (intention: Intention)
            : ProcessResult =

        let thisUnchanged = unchanged objectClientMap world

        match intention.value.t with
        | Intention.Move (id, dir, amount) ->
            // May generate an event to move the object to its target location
            let clientOwnsObject =
                objectClientMap
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
                            objectClientMap = objectClientMap
                        }
                    else thisUnchanged
                else
                    thisUnchanged

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
                world = newWorld
                objectClientMap = newClientObjectMap
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
                world = updatedWorld
                objectClientMap = updatedObjectClientMap
            }

    let processMany
            (clientObjectMap: Map<Id.WorldObject, Id.Client>)
            (world: World)
            (intentions: Intention seq)
            : ProcessResult =
        let initial = unchanged clientObjectMap world

        intentions
        |> Seq.fold (fun acc i ->
            let resultOne = processOne acc.objectClientMap acc.world i

            {
                events = Seq.append resultOne.events acc.events
                world = resultOne.world
                objectClientMap = resultOne.objectClientMap
            }
        ) initial
