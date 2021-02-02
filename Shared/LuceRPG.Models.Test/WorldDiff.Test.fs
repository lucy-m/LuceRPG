namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module WorldDiff =

    [<Test>]
    let ``incorrect spawn point`` () =
        let world1 = World.empty [] (Point.create 1 1)
        let world2 = World.empty [] (Point.create 2 1)

        let diff = WorldDiff.diff world1 world2 |> Seq.toList
        let expected = [ WorldDiff.DiffType.IncorrectSpawnPoint]

        diff |> should equal expected

    [<Test>]
    let ``bounds`` () =
        let bounds = [ Rect.create 0 0 1 1; Rect.create 1 1 3 3]
        let extra = Rect.create 4 4 1 1
        let missing = Rect.create 5 4 1 1

        let world1 = World.empty (missing::bounds) Point.zero
        let world2 = World.empty (extra::bounds) Point.zero

        let diff = WorldDiff.diff world1 world2 |> Seq.toList
        let expected =
            [
                WorldDiff.DiffType.ExtraBound extra
                WorldDiff.DiffType.MissingBound missing
            ]

        diff |> should equal expected

    [<Test>]
    let ``objects`` () =
        let bounds = [ Rect.create 0 10 10 10 ]
        let extraObject =
            WorldObject.create WorldObject.Type.Wall (Point.create 0 10)
            |> WithId.create

        let missingObject =
            WorldObject.create WorldObject.Type.Wall (Point.create 2 10)
            |> WithId.create

        let matchingObject =
            WorldObject.create WorldObject.Type.Wall (Point.create 4 10)
            |> WithId.create

        let unmatchingObject1 =
            WorldObject.create WorldObject.Type.Wall (Point.create 6 10)
            |> WithId.create

        let unmatchingObject2 =
            WorldObject.create (WorldObject.Type.Path (1, 1)) (Point.create 8 10)
            |> WithId.useId unmatchingObject1.id

        let world1 =
            World.createWithObjs
                bounds
                Point.zero
                (missingObject::matchingObject::[unmatchingObject1])

        let world2 =
            World.createWithObjs
                bounds
                Point.zero
                (extraObject::matchingObject::[unmatchingObject2])

        let diff = WorldDiff.diff world1 world2

        let expected =
            [
                WorldDiff.DiffType.ExtraObject extraObject.id
                WorldDiff.DiffType.MissingObject missingObject.id
                WorldDiff.DiffType.UnmatchingObject (unmatchingObject1, unmatchingObject2)
            ]

        diff |> should equal expected
