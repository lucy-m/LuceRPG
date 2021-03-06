﻿namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module WorldDiff =
    let name = "test-world"

    [<Test>]
    let ``incorrect spawn point`` () =
        let world1 = World.empty name [] (Point.create 1 1) WorldBackground.GreenGrass
        let world2 = World.empty name [] (Point.create 2 1) WorldBackground.GreenGrass

        let diff = WorldDiff.diff world1 world2 |> Seq.toList
        let expected = [ WorldDiff.DiffType.IncorrectSpawnPoint]

        diff |> should equal expected

    [<Test>]
    let ``bounds`` () =
        let bounds = [ Rect.create 0 0 1 1; Rect.create 1 1 3 3]
        let extra = Rect.create 4 4 1 1
        let missing = Rect.create 5 4 1 1

        let world1 = World.empty name (missing::bounds) Point.zero WorldBackground.GreenGrass
        let world2 = World.empty name (extra::bounds) Point.zero WorldBackground.GreenGrass

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
            WorldObject.create WorldObject.Type.Wall (Point.create 0 10) Direction.South
            |> WithId.create

        let missingObject =
            WorldObject.create WorldObject.Type.Wall (Point.create 2 10) Direction.South
            |> WithId.create

        let matchingObject =
            WorldObject.create WorldObject.Type.Wall (Point.create 4 10) Direction.South
            |> WithId.create

        let unmatchingObject1 =
            WorldObject.create WorldObject.Type.Wall (Point.create 6 10) Direction.South
            |> WithId.create

        let unmatchingObject2 =
            WorldObject.create (WorldObject.Type.Path Point.p1x1) (Point.create 8 10) Direction.South
            |> WithId.useId unmatchingObject1.id

        let world1 =
            World.createWithObjs
                name
                bounds
                Point.zero
                WorldBackground.GreenGrass
                (missingObject::matchingObject::[unmatchingObject1])

        let world2 =
            World.createWithObjs
                name
                bounds
                Point.zero
                WorldBackground.GreenGrass
                (extraObject::matchingObject::[unmatchingObject2])

        let diff = WorldDiff.diff world1 world2

        let expected =
            [
                WorldDiff.DiffType.ExtraObject extraObject.id
                WorldDiff.DiffType.MissingObject missingObject.id
                WorldDiff.DiffType.UnmatchingObjectPosition
                    (unmatchingObject1.id, Point.create 6 10, Point.create 8 10)
            ]

        diff |> should equal expected
