namespace LuceRPG.Models

module IntentionProcessing =
    type ProcessResult =
        {
            events: WorldEvent seq
            world: World
        }

    let unchanged (world: World): ProcessResult =
        {
            events = []
            world = world
        }

    let processOne (intention: Intention) (world: World): ProcessResult =
        match intention.value with
        | Intention.Move (id, dir, amount) ->
            let tObj = world.objects |> Map.tryFind id

            match tObj with
            | Option.None -> unchanged world
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
                    }
                else unchanged world
        | Intention.JoinGame ->
            let spawnPoint = World.spawnPoint world
            let obj = WorldObject.create WorldObject.Type.Player spawnPoint |> WithId.create

            let newWorld = World.addObject obj world
            let event =
                WorldEvent.Type.GameJoined obj.id
                |> WorldEvent.asResult intention.id

            {
                events = [event]
                world = newWorld
            }

    let processMany (intentions: Intention seq) (world: World): ProcessResult =
        let initial = unchanged world

        intentions
        |> Seq.fold (fun acc i ->
            let resultOne = processOne i acc.world

            {
                events = Seq.append resultOne.events acc.events
                world = resultOne.world
            }
        ) initial
