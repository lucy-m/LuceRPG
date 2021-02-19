namespace LuceRPG.Models

module EventApply =
    let apply (event: WorldEvent) (world: World): World =
        if event.world <> world.id
        then world
        else
            match event.t with
            | WorldEvent.Type.Moved (id, dir) ->
                let tObj = world.value.objects |> Map.tryFind id
                tObj
                |> Option.map (fun obj ->
                    let newObj = WithId.map (WorldObject.moveObject dir) obj
                    world |> WithId.map (World.addObject newObj)
                )
                |> Option.defaultValue world

            | WorldEvent.Type.ObjectAdded obj ->
                world |> WithId.map (World.addObject obj)

            | WorldEvent.Type.ObjectRemoved id ->
                world |> WithId.map (World.removeObject id)

            | WorldEvent.Type.JoinedWorld _ -> world
