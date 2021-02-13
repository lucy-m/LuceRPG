namespace LuceRPG.Models

module EventApply =
    let apply (event: WorldEvent) (world: World): World =
        match event.t with
        | WorldEvent.Type.Moved (id, dir) ->
            let tObj = world.value.objects |> Map.tryFind id
            tObj
            |> Option.map (fun obj ->
                let newObj = WorldObject.moveObject dir obj
                WithId.map (World.addObject newObj) world
            )
            |> Option.defaultValue world

        | WorldEvent.Type.ObjectAdded obj ->
            WithId.map (World.addObject obj) world

        | WorldEvent.Type.ObjectRemoved id ->
            WithId.map (World.removeObject id) world
