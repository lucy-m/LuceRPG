namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module Behaviour =

    [<TestFixture>]
    module patrol =

        [<TestFixture>]
        module ``repeating alteranting between movement and wait`` =
            let steps =
                [
                    Behaviour.MovementStep.Move (Direction.North, 3uy)
                    Behaviour.MovementStep.Wait 100L
                ]

            let patrol = Behaviour.patrol steps true Option.None
            let update200 = patrol.update 200L

            [<Test>]
            let ``update produces intention to move north`` () =
                let intentions = fst update200 |> List.ofSeq

                intentions.Length |> should equal 1

                let head = intentions.Head ""

                match head with
                | Intention.Type.Move (id, d, n) ->
                    d |> should equal Direction.North
                    n |> should equal 3uy
                | _ ->
                    failwith "Incorrect case"

            [<Test>]
            let ``behaviour is not complete`` () =
                snd update200 |> Option.isSome |> should equal true

            [<TestFixture>]
            module ``next update`` =
                snd update200 |> Option.isSome |> should equal true

                let update250 = (snd update200).Value.update 250L

                [<Test>]
                let ``produces no intention`` () =
                    fst update250 |> Seq.isEmpty |> should equal true

                [<Test>]
                let ``behaviour is not complete`` () =
                    snd update250 |> Option.isSome |> should equal true

                [<Test>]
                let ``updating again after 99 ticks produces no intention`` () =
                    snd update250 |> Option.isSome |> should equal true
                    let update349 = (snd update250).Value.update 349L
                    fst update349 |> Seq.isEmpty |> should equal true

                [<Test>]
                let ``updating again after 100 ticks produces intention`` () =
                    snd update250 |> Option.isSome |> should equal true
                    let update350 = (snd update250).Value.update 350L
                    fst update350 |> Seq.isEmpty |> should equal false

        [<TestFixture>]
        module ``non-repeating`` =
            let steps =
                [
                    Behaviour.MovementStep.Move (Direction.North, 3uy)
                    Behaviour.MovementStep.Move (Direction.East, 1uy)
                ]

            let patrol = Behaviour.patrol steps false Option.None
            let update1 = patrol.update 1L

            [<Test>]
            let ``produces intention to move north`` () =
                let intentions = fst update1 |> List.ofSeq

                intentions.Length |> should equal 1

                let head = intentions.Head ""

                match head with
                | Intention.Type.Move (id, d, n) ->
                    d |> should equal Direction.North
                    n |> should equal 3uy
                | _ ->
                    failwith "Incorrect case"

            [<Test>]
            let ``behaviour is not complete`` () =
                snd update1 |> Option.isSome |> should equal true

            [<TestFixture>]
            module ``second update`` =
                snd update1 |> Option.isSome |> should equal true
                let update2 = (snd update1).Value.update 2L

                [<Test>]
                let ``produces intention to move east`` () =
                    let intentions = fst update2 |> List.ofSeq

                    intentions.Length |> should equal 1

                    let head = intentions.Head ""

                    match head with
                    | Intention.Type.Move (id, d, n) ->
                        d |> should equal Direction.East
                        n |> should equal 1uy
                    | _ ->
                        failwith "Incorrect case"

                [<TestFixture>]
                module ``third update`` =
                    snd update2 |> Option.isSome |> should equal true
                    let update3 = (snd update2).Value.update 2L

                    [<Test>]
                    let ``behaviour is complete`` () =
                        snd update3 |> Option.isNone |> should equal true

