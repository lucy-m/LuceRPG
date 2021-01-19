namespace LuceRPG.Serialisation

open NUnit.Framework
open FsCheck
open FsUnit
open LuceRPG.Models

[<TestFixture>]
module WorldSrl =

    [<SetUp>]
    let setup () =
        Arb.register<SerialisationArbs>() |> ignore

    [<Test>]
    let ``serialises and deserialises correctly`` () =
        let checkFn (world: World): bool =
            let srl = WorldSrl.serialise world
            let desrl =
                WorldSrl.deserialise srl
                |> DesrlResult.value

            let t = desrl = Option.Some world

            t

        Check.QuickThrowOnFailure checkFn
