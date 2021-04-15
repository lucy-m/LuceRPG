namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module Direction =
    [<Test>]
    let ``rotateN correct`` () =
        let dir = Direction.North

        Direction.rotateCwN dir 1u |> should equal Direction.East
        Direction.rotateCwN dir 2u |> should equal Direction.South
        Direction.rotateCwN dir 3u |> should equal Direction.West
        Direction.rotateCwN dir 4u |> should equal Direction.North
        Direction.rotateCwN dir 5u |> should equal Direction.East
