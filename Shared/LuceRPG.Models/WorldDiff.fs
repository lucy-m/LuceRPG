namespace LuceRPG.Models

module WorldDiff =
    type DiffType =
        | IncorrectSpawnPoint
        | ExtraBound of Rect
        | MissingBound of Rect
        | ExtraObject of Id.WorldObject
        | MissingObject of Id.WorldObject
        | UnmatchingObjectPosition of Id.WorldObject * Point * Point

    type Model = DiffType List

    let diff (fromWorld: World.Payload) (toWorld: World.Payload): DiffType seq =
        let spawnPoint =
            if fromWorld.playerSpawner <> toWorld.playerSpawner
            then seq { DiffType.IncorrectSpawnPoint }
            else Seq.empty

        let bounds =
            let extraBounds =
                toWorld.bounds
                |> Set.filter (fun r ->
                    fromWorld.bounds
                    |> Set.contains r
                    |> not
                )
                |> Set.map DiffType.ExtraBound

            let missingBounds =
                fromWorld.bounds
                |> Set.filter (fun r ->
                    toWorld.bounds
                    |> Set.contains r
                    |> not
                )
                |> Set.map DiffType.MissingBound

            Seq.concat [extraBounds; missingBounds]

        let objects =
            let extraObjects =
                toWorld
                |> World.objectList
                |> List.map WithId.id
                |> List.filter (fun oId ->
                    fromWorld.objects
                    |> Map.containsKey oId
                    |> not
                )
                |> List.map DiffType.ExtraObject

            let matchingIds, missingIds =
                fromWorld
                |> World.objectList
                |> List.map WithId.id
                |> List.partition (fun oId ->
                    toWorld.objects
                    |> Map.containsKey oId
                )

            let missingObjects =
                missingIds
                |> List.map DiffType.MissingObject

            let unmatchingObjects =
                matchingIds
                |> List.map (fun id ->
                    let fromObj = fromWorld.objects |> Map.find id
                    let toObj = toWorld.objects |> Map.find id

                    (fromObj, toObj)
                )
                |> List.filter (fun (fromObj, toObj) -> fromObj <> toObj)

            let unmatchingPositions =
                unmatchingObjects
                |> List.filter (fun (fromObj, toObj) ->
                    fromObj.value.btmLeft <> toObj.value.btmLeft
                )
                |> List.map (fun (fromObj, toObj) ->
                    DiffType.UnmatchingObjectPosition
                        (
                            fromObj.id,
                            fromObj.value.btmLeft,
                            toObj.value.btmLeft
                        )
                )

            Seq.concat [
                extraObjects;
                missingObjects;
                unmatchingPositions
            ]

        Seq.concat [spawnPoint; bounds; objects]

type WorldDiff = WorldDiff.Model
