namespace LuceRPG.Serialisation

open LuceRPG.Models
open FsCheck

type SerialisationArbs() =
    static member genString: Gen<string> =
        Gen.fresh (fun () -> System.Guid.NewGuid().ToString())

    static member genRect: Gen<Rect> =
        let size =
            Gen.choose (5,80)
            |> Gen.two
            |> Gen.map (fun (x,y) -> Point.create x y)

        let topLeft =
            Gen.choose (-100,100)
            |> Gen.two
            |> Gen.map (fun (x,y) -> Point.create x y)

        Gen.zip topLeft size
        |> Gen.map (fun (t, s) -> Rect.pointCreate t s)

    static member genWorldObject: Gen<WorldObject> =
        let id =
            Arb.generate<System.Guid>
            |> Gen.map (fun g -> g.ToString())

        let topLeft =
            Gen.choose (-100,100)
            |> Gen.two
            |> Gen.map (fun (x,y) -> Point.create x y)

        let objType =
            Arb.generate<WorldObject.Type>

        Gen.zip3 id objType topLeft
        |> Gen.map (fun (id, t, p) -> WorldObject.create t p |> WithId.useId id)

    static member genWorld: Gen<World> =
        let bounds = Gen.listOf Arb.generate<Rect>
        let point =
            bounds
            |> Gen.map (fun rects ->
                List.tryHead rects
                |> Option.map (fun r -> r.topLeft)
                |> Option.defaultValue Point.zero
            )
        let objects = Gen.listOf Arb.generate<WorldObject>
        let interactions =
            let objectIds =
                objects
                |> Gen.map (fun objs ->
                    objs
                    |> List.map (fun o -> o.id)
                    |> List.take (objs.Length / 2)
                )

            objectIds
            |> Gen.map (fun oIds ->
                oIds
                |> List.map (fun oId ->
                    let iId = Arb.generate<string> |> Gen.sample 0 1 |> List.head
                    (oId, iId)
                )
                |> Map.ofList
            )

        let world =
            Gen.zip (Gen.zip bounds point)
                    (Gen.zip objects interactions)
            |> Gen.map (fun ((bs, p), (os, is)) ->
                World.createWithInteractions bs p os is
            )

        world

    static member genGetJoinGameResult: Gen<GetJoinGameResult> =
        let worldGen =
            Gen.zip3
                (Gen.zip Arb.generate<string> Arb.generate<string>)
                (Gen.zip Arb.generate<int64> Arb.generate<World>)
                Arb.generate<Interaction List>
            |> Gen.map (fun ((cId, oId), (ts, w), il) ->
                let tsWorld = WithTimestamp.create ts w
                let payload = GetJoinGameResult.SuccessPayload.create cId oId tsWorld il
                GetJoinGameResult.Success payload
            )
        let failureGen =
            Arb.generate<string>
            |> Gen.map (fun s -> GetJoinGameResult.Failure s)

        Gen.oneof [worldGen; failureGen]

    static member string (): Arbitrary<string> = Arb.fromGen SerialisationArbs.genString
    static member rect (): Arbitrary<Rect> = Arb.fromGen SerialisationArbs.genRect
    static member worldObject (): Arbitrary<WorldObject> = Arb.fromGen SerialisationArbs.genWorldObject
    static member world (): Arbitrary<World> = Arb.fromGen SerialisationArbs.genWorld
    static member getJoinGameResult (): Arbitrary<GetJoinGameResult> = Arb.fromGen SerialisationArbs.genGetJoinGameResult
