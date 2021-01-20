namespace LuceRPG.Serialisation

open NUnit.Framework
open FsUnit
open FsCheck

[<TestFixture>]
module ListSrl =

    [<Test>]
    let ``works correctly`` () =
        let fn = IntSrl.deserialise
        let bytes = Array.concat([
            // count
            IntSrl.serialise 3
            // items
            IntSrl.serialise 8
            IntSrl.serialise 1
            IntSrl.serialise 2
        ])

        let list = ListSrl.deserialise fn bytes

        list.IsSome |> should equal true
        list.Value.value.Length |> should equal 3
        list.Value.value |> should be (equivalent [8;1;2])

    [<Test>]
    let ``returns None when not given enough data`` () =
        let fn = IntSrl.deserialise
        let bytes = Array.concat([
            // count
            IntSrl.serialise 6
            // items
            IntSrl.serialise 8
            IntSrl.serialise 1
        ])

        let list = ListSrl.deserialise fn bytes

        list.IsSome |> should equal false
