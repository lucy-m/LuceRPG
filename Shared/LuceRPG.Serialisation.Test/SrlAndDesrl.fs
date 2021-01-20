namespace LuceRPG.Serialisation

open NUnit.Framework
open FsCheck
open LuceRPG.Models

[<TestFixture>]
module SrlAndDesrl =

    let srlAndDesrl
        (srlFn: 'T -> byte[])
        (desrlFn: byte[] -> 'T DesrlResult)
        (value: 'T)
        : bool =
            let srl = srlFn value
            let desrl = desrlFn srl |> DesrlResult.value

            let v = desrl = Option.Some value

            desrl = Option.Some value

    let doCheck = Check.QuickThrowOnFailure

    [<TestFixture>]
    module ``serialises and deserialises correctly`` =

        [<SetUp>]
        let setup () =
            Arb.register<SerialisationArbs>() |> ignore

        [<Test>]
        let intSrl () =
            let checkFn =
                srlAndDesrl IntSrl.serialise IntSrl.deserialise

            doCheck checkFn

        [<Test>]
        let listSrl () =
            let checkFn =
                srlAndDesrl
                    (ListSrl.serialise IntSrl.serialise)
                    (ListSrl.deserialise IntSrl.deserialise)

            doCheck checkFn

        [<Test>]
        let pointSrl () =
            let checkFn =
                srlAndDesrl PointSrl.serialise PointSrl.deserialise

            doCheck checkFn

        [<Test>]
        let rectSrl () =
            let checkFn =
                srlAndDesrl RectSrl.serialise RectSrl.deserialise

            doCheck checkFn

        [<Test>]
        let directionSrl () =
            let checkFn =
                srlAndDesrl DirectionSrl.serialise DirectionSrl.deserialise

            doCheck checkFn

        [<Test>]
        let worldObjectTypeSrl () =
            let checkFn =
                srlAndDesrl WorldObjectSrl.serialiseType WorldObjectSrl.deserialiseType

            doCheck checkFn

        [<Test>]
        let worldObjectSrl () =
            let checkFn =
                srlAndDesrl WorldObjectSrl.serialise WorldObjectSrl.deserialise

            doCheck checkFn

        [<Test>]
        let intentionSrl () =
            let checkFn =
                srlAndDesrl IntentionSrl.serialise IntentionSrl.deserialise

            doCheck checkFn

        [<Test>]
        let worldSrl () =
            let checkFn =
                srlAndDesrl WorldSrl.serialise WorldSrl.deserialise

            doCheck checkFn

        [<Test>]
        let ``manual test`` () =
            let srlAndDesrl
                (srlFn: World -> byte[])
                (desrlFn: byte[] -> World DesrlResult)
                (value: World)
                : bool =
                    let srl = srlFn value
                    let desrl = desrlFn srl |> DesrlResult.value

                    let v = desrl = Option.Some value
                    let boundMatch = (Option.get desrl).bounds = value.bounds
                    let objectsMatch = (Option.get desrl).objects = value.objects
                    let blockedMatch = (Option.get desrl).blocked = value.blocked

                    desrl = Option.Some value

            let checkFn =
                srlAndDesrl WorldSrl.serialise WorldSrl.deserialise

            Check.QuickThrowOnFailure checkFn
