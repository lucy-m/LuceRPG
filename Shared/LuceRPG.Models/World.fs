namespace LuceRPG.Models

module World =
    type Model =
        {
            bounds: Rect Set
            objects: Map<Id.WorldObject, WorldObject>
            blocked: Map<Point, WorldObject>
            playerSpawner: Point
        }

    let objectList (world: Model): WorldObject List =
        world.objects
        |> Map.toList
        |> List.map snd

    let empty (bounds: Rect List) (playerSpawner: Point): Model =
        {
            bounds = bounds |> Set.ofList
            objects = Map.empty
            blocked = Map.empty
            playerSpawner = playerSpawner
        }

    let pointBlocked (p: Point) (world: Model): bool =
        world.blocked
        |> Map.containsKey p

    let getBlocker (p: Point) (world: Model): WorldObject Option =
        world.blocked |> Map.tryFind p

    let pointInBounds (p: Point) (world: Model): bool =
        let containingRect =
            world.bounds
            |> Set.toList
            |> List.tryFind (Rect.contains p)

        Option.isSome containingRect

    let objInBounds (obj: WorldObject) (world: Model): bool =
        let points = WorldObject.getPoints obj.value

        points
        |> List.map (fun p -> pointInBounds p world)
        |> List.fold (&&) true

    let containsObject (id: Id.WorldObject) (world: Model): bool =
        world.objects
        |> Map.containsKey id

    let canPlace (obj: WorldObject) (world: Model): bool =
        let points = WorldObject.getPoints obj.value

        let isNotBlocked =
            let blockedPoints =
                points
                |> List.choose (fun p -> getBlocker p world)
                |> List.filter (fun wo -> wo.id <> obj.id)
            blockedPoints |> List.isEmpty

        let inBounds = objInBounds obj world

        isNotBlocked && inBounds

    let removeObject (id: Id.WorldObject) (world: Model): Model =
        let newObjects =
            world.objects
            |> Map.remove id

        let newBlocked =
            world.blocked
            |> Map.filter (fun _ wo ->
                wo.id <> id
            )

        {
            world with
                objects = newObjects
                blocked = newBlocked
        }

    let spawnPoint (world: Model): Point =
        world.playerSpawner

    /// Adds an object to the map
    /// Object will not be added if it is blocked or out of bounds
    /// An object with the same id that already exists will be removed
    /// Blocking objects can be placed on top of non-blocking objects
    let addObject (obj: WorldObject) (world: Model): Model =
        let existingIdRemoved = removeObject obj.id world

        let canPlaceObject = canPlace obj existingIdRemoved

        let points = WorldObject.getPoints obj.value

        if not canPlaceObject
        then existingIdRemoved
        else
            let isBlocking = WorldObject.isBlocking obj

            let blocked =
                if isBlocking
                then
                    // add all points to blocking map
                    points
                    |> List.fold
                        (fun acc p -> Map.add p obj acc)
                        existingIdRemoved.blocked
                else
                    existingIdRemoved.blocked

            let objects =
                    Map.add obj.id obj existingIdRemoved.objects

            {
                world with
                    blocked = blocked
                    objects = objects
            }

    /// Adds many objects
    /// Blocking objects will be added first
    /// Invalid objects will be ignored
    let addObjects (objs: WorldObject List) (world: Model): Model =
        let blocking, nonBlocking =
            objs
            |> List.partition WorldObject.isBlocking

        let withItems =
            (blocking @ nonBlocking)
            |> List.fold (fun acc o -> addObject o acc) world

        withItems

    let createWithObjs (bounds: Rect List) (spawn: Point) (objs: WorldObject List): Model =
        let emptyWorld = empty bounds spawn
        addObjects objs emptyWorld

type World = World.Model
