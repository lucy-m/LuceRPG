namespace LuceRPG.Serialisation

open NUnit.Framework
open FsUnit
open FsCheck
open LuceRPG.Models

[<TestFixture>]
module WorldObjectSrl =

    [<Test>]
    let ``returns none for unknown world object type `` () =
        let unknown = 200uy;
        let desrl = WorldObjectSrl.deserialiseType([|unknown|])

        desrl.IsNone |> should equal true

