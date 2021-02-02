namespace LuceRPG.Serialisation

open LuceRPG.Models

module WorldDiffSrl =

    let serialiseDiffType (diffType: WorldDiff.DiffType): byte[] =
        let label =
            match diffType with
            | WorldDiff.DiffType.IncorrectSpawnPoint -> 1uy
            | WorldDiff.DiffType.ExtraBound _ -> 2uy
            | WorldDiff.DiffType.MissingBound _ -> 3uy
            | WorldDiff.DiffType.ExtraObject _ -> 4uy
            | WorldDiff.DiffType.MissingObject _ -> 5uy
            | WorldDiff.DiffType.UnmatchingObject _ -> 6uy

        let addtInfo =
            match diffType with
            | WorldDiff.DiffType.IncorrectSpawnPoint -> [||]
            | WorldDiff.DiffType.ExtraBound r -> RectSrl.serialise r
            | WorldDiff.DiffType.MissingBound r -> RectSrl.serialise r
            | WorldDiff.DiffType.ExtraObject id -> StringSrl.serialise id
            | WorldDiff.DiffType.MissingObject id -> StringSrl.serialise id
            | WorldDiff.DiffType.UnmatchingObject (obj1, obj2) ->
                let s1 = WorldObjectSrl.serialise obj1
                let s2 = WorldObjectSrl.serialise obj2

                Array.append s1 s2

        Array.append [|label|] addtInfo

    let serialise (worldDiff: WorldDiff): byte[] =
        ListSrl.serialise serialiseDiffType worldDiff

    let deserialiseDiffType (bytes: byte[]): WorldDiff.DiffType DesrlResult =
        let loadObj (tag: byte) (objectBytes: byte[]): WorldDiff.DiffType DesrlResult =
            match tag with
            | 1uy ->
                DesrlResult.create WorldDiff.DiffType.IncorrectSpawnPoint 0
            | 2uy ->
                RectSrl.deserialise objectBytes
                |> DesrlResult.map WorldDiff.DiffType.ExtraBound
            | 3uy ->
                RectSrl.deserialise objectBytes
                |> DesrlResult.map WorldDiff.DiffType.MissingBound
            | 4uy ->
                StringSrl.deserialise objectBytes
                |> DesrlResult.map WorldDiff.DiffType.ExtraObject
            | 5uy ->
                StringSrl.deserialise objectBytes
                |> DesrlResult.map WorldDiff.DiffType.MissingObject
            | 6uy ->
                DesrlUtil.getTwo
                    WorldObjectSrl.deserialise
                    WorldObjectSrl.deserialise
                    (fun obj1 obj2 -> WorldDiff.DiffType.UnmatchingObject(obj1, obj2))
                    objectBytes
            | _ ->
                printfn "Unknown WorldDiff DiffType tag %u" tag
                Option.None

        DesrlUtil.getTagged loadObj bytes

    let deserialise (bytes: byte[]): WorldDiff DesrlResult =
        ListSrl.deserialise deserialiseDiffType bytes
