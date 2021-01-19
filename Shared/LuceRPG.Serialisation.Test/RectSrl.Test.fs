namespace LuceRPG.Serialisation

open NUnit.Framework
open FsCheck
open LuceRPG.Models

[<TestFixture>]
module RectSrl =

    [<Test>]
    let ``serialises and deserialises correctly`` () =
        let checkFn (r: Rect): bool =
            let srl = RectSrl.serialise r
            let desrl =
                RectSrl.deserialise srl
                |> DesrlResult.value

            desrl = Option.Some r

        Check.QuickThrowOnFailure checkFn
