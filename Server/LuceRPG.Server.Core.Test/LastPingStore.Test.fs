namespace LuceRPG.Server.Core

open NUnit.Framework
open FsUnit
open LuceRPG.Models

[<TestFixture>]
module LastPingStore =

    [<TestFixture>]
    module ``for a store with a stale and fresh client`` =
        let staleThreshold = 10L
        let staleClient = ("client1", 8L)
        let freshClient = ("client2", 12L)

        let model =
            [ staleClient; freshClient ]
            |> Map.ofList

        [<TestFixture>]
        module ``culling`` =
            let cullResult = LastPingStore.cull staleThreshold model

            [<Test>]
            let ``removes staleClient from the map`` () =
                Map.tryFind (fst staleClient) cullResult.updated
                |> Option.isNone
                |> should equal true

            [<Test>]
            let ``does not remove freshClient from the map`` () =
                Map.tryFind (fst freshClient) cullResult.updated
                |> Option.isSome
                |> should equal true

            [<Test>]
            let ``generates a leave game intention`` () =
                let intentions = cullResult.intentions |> Seq.toList
                let expected = Intention.makePayload (fst staleClient) Intention.Type.LeaveGame

                intentions.Length |> should equal 1
                intentions.Head.value |> should equal expected
