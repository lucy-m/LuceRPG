namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module Direction =
    [<Test>]
    let ``rotateCwN correct`` () =
        let dir = Direction.North

        Direction.rotateCwN 1 dir |> should equal Direction.East
        Direction.rotateCwN 2 dir |> should equal Direction.South
        Direction.rotateCwN 3 dir |> should equal Direction.West
        Direction.rotateCwN 4 dir |> should equal Direction.North
        Direction.rotateCwN 5 dir |> should equal Direction.East

        Direction.rotateCwN -1 dir |> should equal Direction.West
        Direction.rotateCwN -2 dir |> should equal Direction.South

    [<Test>]
    let ``fromInt correct`` () =
        Direction.fromInt 0 |> should equal Direction.North
        Direction.fromInt 1 |> should equal Direction.East
        Direction.fromInt 2 |> should equal Direction.South
        Direction.fromInt 3 |> should equal Direction.West

        Direction.fromInt 4 |> should equal Direction.North
        Direction.fromInt 5 |> should equal Direction.East
        Direction.fromInt 6 |> should equal Direction.South
        Direction.fromInt 7 |> should equal Direction.West

        Direction.fromInt -1 |> should equal Direction.West
        Direction.fromInt -2 |> should equal Direction.South
        Direction.fromInt -3 |> should equal Direction.East
        Direction.fromInt -4 |> should equal Direction.North
