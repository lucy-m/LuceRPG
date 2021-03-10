namespace LuceRPG.Models

module World =
    type BlockedType =
        | Object of WorldObject
        | SpawnPoint

    type InteractionMap = Map<Id.WorldObject, Id.Interaction>

    type Payload =
        {
            name: string
            bounds: Rect Set
            objects: Map<Id.WorldObject, WorldObject>
            interactions: InteractionMap
            blocked: Map<Point, BlockedType>
            warps: Map<Point, Id.WorldObject * WorldObject.WarpData>
            playerSpawner: Point
        }

    let objectList (world: Payload): WorldObject List =
        WithId.toList world.objects

    let empty (name: string) (bounds: Rect seq) (playerSpawner: Point): Payload =
        let blocked =
            let spawnerPoints =
                [
                    Point.zero
                    Point.create 0 1
                    Point.create 1 0
                    Point.create 1 1
                ]
                |> List.map (Point.add playerSpawner)

            spawnerPoints
            |> List.map (fun p -> (p, BlockedType.SpawnPoint))
            |> Map.ofList

        {
            name = name
            bounds = bounds |> Set.ofSeq
            objects = Map.empty
            interactions = Map.empty
            blocked = blocked
            warps = Map.empty
            playerSpawner = playerSpawner
        }

    let pointBlocked (p: Point) (world: Payload): bool =
        world.blocked
        |> Map.containsKey p

    let getBlocker (p: Point) (world: Payload): BlockedType Option =
        world.blocked |> Map.tryFind p

    let pointInBounds (p: Point) (world: Payload): bool =
        let containingRect =
            world.bounds
            |> Set.toList
            |> List.tryFind (Rect.contains p)

        Option.isSome containingRect

    let objInBounds (obj: WorldObject) (world: Payload): bool =
        let points = WorldObject.getPoints obj.value

        points
        |> Seq.map (fun p -> pointInBounds p world)
        |> Seq.fold (&&) true

    let containsObject (id: Id.WorldObject) (world: Payload): bool =
        world.objects
        |> Map.containsKey id

    let canPlace (obj: WorldObject) (world: Payload): bool =
        let points = WorldObject.getPoints obj.value

        let isNotBlocked =
            let blockedPoints =
                points
                |> Seq.choose (fun p -> getBlocker p world)
                |> Seq.filter (fun b ->
                    match b with
                    // objects are blocked by other objects with a differing id
                    | BlockedType.Object o -> o.id <> obj.id
                    // players are not blocked by spawn points
                    | BlockedType.SpawnPoint _ -> not (WorldObject.isPlayer obj.value)
                )

            blockedPoints |> Seq.isEmpty

        let inBounds = objInBounds obj world

        isNotBlocked && inBounds

    let getWarp (id: Id.WorldObject) (world: Payload): WorldObject.WarpData Option =
        let tObject = world.objects |> Map.tryFind id
        let tPoints = tObject |> Option.map (WithId.value >> WorldObject.getPoints)
        let tWarp =
            tPoints
            |> Option.bind (fun points ->
                points
                |> Seq.tryPick (fun p -> world.warps |> Map.tryFind p)
            )

        tWarp |> Option.map (fun (objId, wd) -> wd)

    let removeObject (id: Id.WorldObject) (world: Payload): Payload =
        let newObjects =
            world.objects
            |> Map.remove id

        let newBlocked =
            world.blocked
            |> Map.filter (fun _ b ->
                match b with
                | BlockedType.Object wo -> wo.id <> id
                | BlockedType.SpawnPoint _ -> true
            )

        let newInteractions =
            world.interactions
            |> Map.remove id

        let newWarps =
            world.warps
            |> Map.filter (fun p (oId, wd) ->
                oId <> id
            )

        {
            world with
                objects = newObjects
                blocked = newBlocked
                interactions = newInteractions
                warps = newWarps
        }

    let spawnPoint (world: Payload): Point =
        world.playerSpawner

    let getInteraction
            (objectId: Id.WorldObject)
            (interactions: Interaction.Store)
            (world: Payload)
            : Interaction Option =
        let interactionId =
            world.interactions
            |> Map.tryFind objectId

        interactionId
        |> Option.bind (fun iId ->
            interactions |> Map.tryFind iId
        )

    /// Adds an object to the map
    /// Object will not be added if it is blocked or out of bounds
    /// An object with the same id that already exists will be removed
    /// Blocking objects can be placed on top of non-blocking objects
    let addObject (obj: WorldObject) (world: Payload): Payload =
        let rec addObjectInner
                (objs: WorldObject List)
                (world: Payload)
                : Payload =

            match objs with
            | [] -> world
            | obj::rem ->
                let existingIdRemoved = removeObject obj.id world

                let canPlaceObject = canPlace obj existingIdRemoved

                let points = WorldObject.getPoints obj.value

                if not canPlaceObject
                then existingIdRemoved
                else
                    let isBlocking = WorldObject.isBlocking obj.value

                    let blocked =
                        if isBlocking
                        then
                            // add all points to blocking map
                            points
                            |> Seq.fold
                                (fun acc p -> Map.add p (BlockedType.Object obj) acc)
                                existingIdRemoved.blocked
                        else
                            existingIdRemoved.blocked

                    let objects =
                        Map.add obj.id obj existingIdRemoved.objects

                    let warps =
                        match obj.value.t with
                        | WorldObject.Type.Warp wd ->
                            // Add the warp to the warps map
                            points
                            |> Seq.fold (fun acc p ->
                                acc |> Map.add p (obj.id, wd)
                            ) world.warps
                        | _ -> world.warps

                    let remaining =
                        match obj.value.t with
                        | WorldObject.Type.Inn tWd ->
                            match tWd with
                            | Option.None -> rem
                            | Option.Some wd ->
                                let warpLocation = Point.add obj.value.btmLeft (Point.create 3 1)
                                let warpId = obj.id + "-warp"
                                let warp =
                                    WorldObject.create (WorldObject.Type.Warp wd) warpLocation Direction.South
                                    |> WithId.useId warpId
                                warp :: rem
                        | _ -> rem

                    let newWorld =
                        {
                            world with
                                blocked = blocked
                                objects = objects
                                warps = warps
                        }

                    addObjectInner remaining newWorld

        addObjectInner [obj] world

    /// Adds many objects
    /// Blocking objects will be added first
    /// Invalid objects will be ignored
    let addObjects (objs: WorldObject seq) (world: Payload): Payload =
        let blocking, nonBlocking =
            objs
            |> List.ofSeq
            |> List.partition (WithId.value >> WorldObject.isBlocking)

        let withItems =
            (blocking @ nonBlocking)
            |> List.fold (fun acc o -> addObject o acc) world

        withItems

    let setInteractions (interactions: InteractionMap) (world: Payload): Payload =
        let validInteractions =
            interactions
            |> Map.filter (fun oId iId ->
                world.objects |> Map.containsKey oId
            )

        {
            world with
                interactions = validInteractions
        }

    let createWithObjs
            (name: string)
            (bounds: Rect seq)
            (spawn: Point)
            (objs: WorldObject seq)
            : Payload =

        let emptyWorld = empty name bounds spawn
        addObjects objs emptyWorld

    let createWithInteractions
            (name: string)
            (bounds: Rect seq)
            (spawn: Point)
            (objs: WorldObject seq)
            (interactions: Map<Id.WorldObject, Id.Interaction>)
            : Payload =
        let emptyWorld = empty name bounds spawn
        let withObjects = addObjects objs emptyWorld
        setInteractions interactions withObjects

    type Model = Payload WithId
    type Map = Map<Id.World, Model>

type World = World.Model
