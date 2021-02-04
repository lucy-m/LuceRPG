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
        let stringSrl () =
            let checkFn =
                srlAndDesrl StringSrl.serialise StringSrl.deserialise

            doCheck checkFn

        [<Test>]
        let listSrl () =
            let checkFn =
                srlAndDesrl
                    (ListSrl.serialise IntSrl.serialise)
                    (ListSrl.deserialise IntSrl.deserialise)

            doCheck checkFn

        [<Test>]
        let mapSrl () =
            let checkFn =
                srlAndDesrl
                    (MapSrl.serialise IntSrl.serialise StringSrl.serialise)
                    (MapSrl.deserialise IntSrl.deserialise StringSrl.deserialise)

            doCheck checkFn

        [<Test>]
        let withTimestampSrl () =
            let checkFn =
                srlAndDesrl
                    (WithTimestampSrl.serialise IntSrl.serialise)
                    (WithTimestampSrl.deserialise IntSrl.deserialise)

            doCheck checkFn

        [<Test>]
        let withIdSrl () =
            let checkFn =
                srlAndDesrl
                    (WithIdSrl.serialise IntSrl.serialise)
                    (WithIdSrl.deserialise IntSrl.deserialise)

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
        let indexedIntentionSrl () =
            let checkFn =
                srlAndDesrl IntentionSrl.serialiseIndexed IntentionSrl.deserialiseIndexed

            doCheck checkFn

        [<Test>]
        let worldEventSrl () =
            let checkFn =
                srlAndDesrl WorldEventSrl.serialise WorldEventSrl.deserialise

            doCheck checkFn

        [<Test>]
        let worldSrl () =
            let checkFn =
                srlAndDesrl WorldSrl.serialise WorldSrl.deserialise

            doCheck checkFn

        [<Test>]
        let worldDiffDiffTypeSrl () =
            let checkFn =
                srlAndDesrl WorldDiffSrl.serialiseDiffType WorldDiffSrl.deserialiseDiffType

            doCheck checkFn

        [<Test>]
        let worldDiffSrl () =
            let checkFn =
                srlAndDesrl WorldDiffSrl.serialise WorldDiffSrl.deserialise

            doCheck checkFn

        [<Test>]
        let interactionSrl () =
            let checkFn =
                srlAndDesrl InteractionSrl.serialise InteractionSrl.deserialise

            doCheck checkFn

        [<Test>]
        let getSinceResultSrl () =
            let checkFn =
                srlAndDesrl GetSinceResultSrl.serialise GetSinceResultSrl.deserialise

            doCheck checkFn

        [<Test>]
        let getJoinGameResultSrl () =
            let checkFn =
                srlAndDesrl
                    GetJoinGameResultSrl.serialise
                    GetJoinGameResultSrl.deserialise

            doCheck checkFn

        [<Test>]
        let clientLogEntrySrl () =
            let checkFn =
                srlAndDesrl ClientLogEntrySrl.serialise ClientLogEntrySrl.deserialise

            doCheck checkFn
