namespace LuceRPG.Serialisation

open NUnit.Framework
open FsCheck

[<TestFixture>]
module IntSrl =

    [<Test>]
    let ``serialises and deserialises correctly`` () =
        let checkFn (n: int): bool =
            let srl = IntSrl.serialise n
            let desrl =
                IntSrl.deserialise srl
                |> DesrlResult.value

            desrl = Option.Some n

        Check.QuickThrowOnFailure checkFn
