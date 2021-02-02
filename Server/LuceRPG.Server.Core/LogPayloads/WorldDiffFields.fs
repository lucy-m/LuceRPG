namespace LuceRPG.Server.Core

open LuceRPG.Models

module WorldDiffFields =

    let rStr (r: Rect): string =
        sprintf "%i %i %i %i" r.topLeft.x r.topLeft.y r.size.x r.size.y

    let create (dt: WorldDiff.DiffType): string * string seq =
        match dt with
        | WorldDiff.DiffType.IncorrectSpawnPoint ->
            "Incorrect Spawn Point", Seq.empty

        | WorldDiff.DiffType.ExtraBound r ->
             "Extra Bound", seq { rStr r }

        | WorldDiff.DiffType.MissingBound r ->
            "Missing Bound", seq { rStr r }

        | WorldDiff.DiffType.ExtraObject oId ->
            let idStr = sprintf "%s" oId
            "Extra Object", seq { idStr }

        | WorldDiff.DiffType.MissingObject oId ->
            let idStr = sprintf "%s" oId
            "Missing Object", seq { idStr }

        | WorldDiff.DiffType.UnmatchingObjectPosition (id, p1, p2) ->
            let idStr = sprintf "%s" id
            let p1Str = sprintf "%i %i" p1.x p1.y
            let p2Str = sprintf "%i %i" p2.x p2.y

            "Unmatching Object Position", seq { idStr; p1Str; p2Str }

