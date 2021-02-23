namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module EventApply =
    let worldId = "world-id"
    let bounds = [ Rect.create 0 0 10 10]
    let obj = WithId.create (WorldObject.create WorldObject.Type.Wall (Point.create 4 4))
    let world = WithId.useId worldId (World.empty "test" bounds Point.zero)
    let withObj = WithId.map (World.addObject obj) world

    [<Test>]
    let ``apply with matching world id succeeds`` () =
        let event =
            WorldEvent.asResult
                "intention-id"
                worldId
                0
                (WorldEvent.Type.ObjectAdded obj)

        let applied = EventApply.apply event world

        applied |> should equal withObj

    [<Test>]
    let ``apply with non-matching world id is ignored`` () =
        let event =
            WorldEvent.asResult
                "intention-id"
                "other-world-id"
                0
                (WorldEvent.Type.ObjectAdded obj)

        let applied = EventApply.apply event world

        applied |> should equal world

