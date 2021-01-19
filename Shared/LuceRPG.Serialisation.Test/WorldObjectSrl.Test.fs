namespace LuceRPG.Serialisation

open NUnit.Framework
open FsUnit
open FsCheck
open LuceRPG.Models

[<TestFixture>]
module WorldObjectSrl =

    [<Test>]
    let ``serialises and deserialises type correctly`` () =
        let checkFn (t: WorldObject.Type): bool =
            let srl = WorldObjectSrl.serialiseType t
            let desrl =
                WorldObjectSrl.deserialiseType srl
                |> DesrlResult.value

            desrl = Option.Some t

        Check.QuickThrowOnFailure checkFn

    [<Test>]
    let ``returns none for unknown world object type `` () =
        let unknown = 200uy;
        let desrl = WorldObjectSrl.deserialiseType([|unknown|])

        desrl.IsNone |> should equal true

    [<Test>]
    let ``serialises and deserialises correctly`` () =
        let checkFn (wo: WorldObject): bool =
            let srl = WorldObjectSrl.serialise wo
            let desrl =
                WorldObjectSrl.deserialise srl
                |> DesrlResult.value

            desrl = Option.Some wo

        Check.QuickThrowOnFailure checkFn
