namespace LuceRPG.Models

module World =
    type Model =
        {
            bounds: Rect List
            objects: WorldObject List
            blocked: Map<Point, WorldObject>
        }

    let empty (bounds: Rect List): Model =
        {
            bounds = bounds
            objects = []
            blocked = Map.empty
        }

    let pointBlocked (p: Point) (world: Model): bool =
        world.blocked
        |> Map.containsKey p

    let pointInBounds (p: Point) (world: Model): bool =
        let containingRect =
            world.bounds
            |> List.tryFind (Rect.contains p)

        Option.isSome containingRect

    let objInBounds (obj: WorldObject) (world: Model): bool =
        let points = WorldObject.getPoints obj

        points
        |> List.map (fun p -> pointInBounds p world)
        |> List.fold (&&) true

    /// Adds an object to the map
    /// Fails if the object is blocked or out of bounds
    let addObject (obj: WorldObject) (world: Model): Model Option =
        let points = WorldObject.getPoints obj
        let isBlocked =
            points
            |> List.map (fun p -> pointBlocked p world)
            |> List.fold (||) false

        let inBounds = objInBounds obj world

        if isBlocked || not inBounds
        then Option.None
        else
            let isBlocking = WorldObject.isBlocking obj

            let blocked =
                if isBlocking
                then
                    // add all points to blocking map
                    points
                    |> List.fold
                        (fun acc p -> Map.add p obj acc)
                        world.blocked
                else
                    world.blocked

            let objects = obj :: world.objects

            {
                world with
                    blocked = blocked
                    objects = objects
            }
            |> Option.Some

type World = World.Model
