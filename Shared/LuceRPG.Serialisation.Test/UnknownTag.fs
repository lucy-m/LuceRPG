namespace LuceRPG.Serialisation

open NUnit.Framework
open FsUnit

[<TestFixture>]
module UnknownTag =

    let unknownTag (desrlFn: byte[] -> 'T DesrlResult) (unknown: byte): unit =
        let desrl = desrlFn [|unknown|]
        desrl.IsNone |> should equal true

    [<TestFixture>]
    module ``returns none for unknown tag`` =

        [<Test>]
        let directionSrl () =
            unknownTag DirectionSrl.deserialise 100uy

        [<Test>]
        let intentionSrl () =
            unknownTag IntentionSrl.deserialise 100uy

        [<Test>]
        let worldObjectTypeSrl () =
            unknownTag WorldObjectSrl.deserialiseType 200uy

        [<Test>]
        let worldDiffTypeSrl () =
            unknownTag WorldDiffSrl.deserialiseDiffType 100uy

        [<Test>]
        let getJoinGameResultSrl () =
            unknownTag GetJoinGameResultSrl.deserialise 100uy

        [<Test>]
        let clientLogEntrySrl () =
            unknownTag ClientLogEntrySrl.deserialise 100uy
