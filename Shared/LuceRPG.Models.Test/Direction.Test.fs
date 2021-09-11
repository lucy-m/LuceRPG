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

    [<Test>]
    let ``sort correct`` () =
        let sample = [ 0,0; 2,1; 1,6; 5,-1 ] |> Point.toPointSet

        let north = [ 5,-1; 0,0; 2,1; 1,6 ] |> Point.toPointSeq
        let south = north |> Seq.rev

        let east = [ 0,0; 1,6; 2,1; 5,-1 ] |> Point.toPointSeq
        let west = east |> Seq.rev

        Direction.sortPoints Direction.North sample |> should equal north
        Direction.sortPoints Direction.South sample |> should equal south
        Direction.sortPoints Direction.East sample |> should equal east
        Direction.sortPoints Direction.West sample |> should equal west

