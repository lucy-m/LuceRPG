namespace LuceRPG.Models

module IntentionProcessing =
    type ProcessResult =
        {
            events: WorldEvent List
            world: World
        }

    let unchanged (world: World): ProcessResult =
        {
            events = []
            world = world
        }

    let processOne (intention: Intention) (world: World): ProcessResult =
        match intention with
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
                    let event = WorldEvent.Model.Moved (id, dir, amount)

                    {
                        events = [event]
                        world = newWorld
                    }
                else unchanged world
