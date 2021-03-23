namespace LuceRPG.Models

open NUnit.Framework
open FsUnit

[<TestFixture>]
module BehaviourMap =
    let moveNorthInfinite =
        Behaviour.patrol [Behaviour.MovementStep.Move (Direction.North, 1uy)] true Option.None
    let emptyBehaviour = Behaviour.patrol [] false Option.None
    let objectId = "object"

    [<TestFixture>]
    module ``for an object that is busy`` =
        let now = 100L
        let objectBusyMap = [objectId, 200L] |> Map.ofList
        let behaviourMap = [objectId, moveNorthInfinite] |> Map.ofList

        let updated = BehaviourMap.update now objectBusyMap behaviourMap

        [<Test>]
        let ``no intentions produced`` () =
            updated.intentions |> Seq.isEmpty |> should equal true

        [<Test>]
        let ``object is still in the map`` () =
            updated.model |> Map.containsKey objectId |> should equal true

    [<TestFixture>]
    module ``for an object that was busy`` =
        let now = 300L
        let objectBusyMap = [objectId, 200L] |> Map.ofList
        let behaviourMap = [objectId, moveNorthInfinite] |> Map.ofList

        let updated = BehaviourMap.update now objectBusyMap behaviourMap

        [<Test>]
        let ``intention produced`` () =
            updated.intentions |> Seq.length |> should equal 1

        [<Test>]
        let ``object is still in the map`` () =
            updated.model |> Map.containsKey objectId |> should equal true

    [<TestFixture>]
    module ``for an object not in the busy map`` =
        let now = 300L
        let objectBusyMap = Map.empty
        let behaviourMap = [objectId, moveNorthInfinite] |> Map.ofList

        let updated = BehaviourMap.update now objectBusyMap behaviourMap

        [<Test>]
        let ``intention produced`` () =
            updated.intentions |> Seq.length |> should equal 1

        [<Test>]
        let ``object is still in the map`` () =
            updated.model |> Map.containsKey objectId |> should equal true

    [<TestFixture>]
    module ``for an object that has completed its behaviour`` =
        let now = 300L
        let objectBusyMap = Map.empty
        let behaviourMap = [objectId, emptyBehaviour] |> Map.ofList

        let updated = BehaviourMap.update now objectBusyMap behaviourMap

        [<Test>]
        let ``no intentions produced`` () =
            updated.intentions |> Seq.isEmpty |> should equal true

        [<Test>]
        let ``object is removed from the map`` () =
            updated.model |> Map.containsKey objectId |> should equal false

    [<TestFixture>]
    module ``for multiple objects`` =
        let now = 300L
        let objectBusyMap = Map.empty
        let behaviourMap =
            [
                "1", moveNorthInfinite
                "2", moveNorthInfinite
                "3", moveNorthInfinite
                "4", emptyBehaviour
            ]
            |> Map.ofList

        let updated = BehaviourMap.update now objectBusyMap behaviourMap

        [<Test>]
        let ``works correctly`` () =
            updated.intentions |> Seq.length |> should equal 3

            updated.model |> Map.count |> should equal 3
