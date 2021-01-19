namespace LuceRPG.Serialisation

open NUnit.Framework
open FsCheck
open LuceRPG.Models

[<TestFixture>]
module PointSrl =

    [<Test>]
    let ``serialises and deserialises correctly`` () =
        let checkFn (p: Point): bool =
            let srl = PointSrl.serialise p
            let desrl =
                PointSrl.deserialise srl
                |> DesrlResult.value

            desrl = Option.Some p

        Check.QuickThrowOnFailure checkFn
