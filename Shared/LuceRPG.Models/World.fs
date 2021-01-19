namespace LuceRPG.Models

module World =
    type Model =
        {
            bounds: Rect Set
            objects: WorldObject Set
            blocked: Map<Point, WorldObject>
        }

    let empty (bounds: Rect List): Model =
        {
            bounds = bounds |> Set.ofList
            objects = Set.empty
            blocked = Map.empty
        }

    let pointBlocked (p: Point) (world: Model): bool =
        world.blocked
        |> Map.containsKey p

    let pointInBounds (p: Point) (world: Model): bool =
        let containingRect =
            world.bounds
            |> Set.toList
            |> List.tryFind (Rect.contains p)

        Option.isSome containingRect

    let objInBounds (obj: WorldObject) (world: Model): bool =
        let points = WorldObject.getPoints obj

        points
        |> List.map (fun p -> pointInBounds p world)
        |> List.fold (&&) true

    /// Adds an object to the map
    /// Fails if the object is blocked or out of bounds
    /// Blocking objects can be placed on top of non-blocking objects
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

            let objects = Set.add obj world.objects

            {
                world with
                    blocked = blocked
                    objects = objects
            }
            |> Option.Some

    /// Adds many objects
    /// Blocking objects will be added first
    /// Invalid objects will be ignored
    let addObjects (objs: WorldObject List) (world: Model): Model Option =
        let blocking, nonBlocking =
            objs
            |> List.partition WorldObject.isBlocking

        let withItems =
            (blocking @ nonBlocking)
            |> List.fold (fun tAcc o ->
                    tAcc
                    |> Option.map (fun acc -> addObject o acc |> Option.defaultValue acc)
            ) (Option.Some world)

        withItems

    let createWithObjs (bounds: Rect List) (objs: WorldObject List): Model =
        let emptyWorld = empty bounds
        addObjects objs emptyWorld |> Option.defaultValue emptyWorld

type World = World.Model
