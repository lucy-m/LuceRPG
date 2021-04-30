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

        let btmLeft =
            Gen.choose (-100,100)
            |> Gen.two
            |> Gen.map (fun (x,y) -> Point.create x y)

        Gen.zip btmLeft size
        |> Gen.map (fun (t, s) -> Rect.pointCreate t s)

    static member genWorldObject: Gen<WorldObject> =
        let id =
            Arb.generate<System.Guid>
            |> Gen.map (fun g -> g.ToString())

        let btmLeft =
            Gen.choose (-100,100)
            |> Gen.two
            |> Gen.map (fun (x,y) -> Point.create x y)

        let facing = Arb.generate<Direction>

        let objType =
            Arb.generate<WorldObject.Type>

        Gen.zip (Gen.zip id objType) (Gen.zip btmLeft facing)
        |> Gen.map (fun ((id, t), (p, f)) -> WorldObject.create t p f |> WithId.useId id)

    static member genWorld: Gen<World.Payload> =
        let name = Arb.generate<string>
        let bounds = Gen.listOf Arb.generate<Rect>
        let point =
            bounds
            |> Gen.map (fun rects ->
                List.tryHead rects
                |> Option.map (fun r -> r.btmLeft)
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

        let dynamicWarps = Arb.generate<Map<Point, Direction>>

        let worldBackground = Arb.generate<WorldBackground>

        let world =
            Gen.zip3
                    name
                    (Gen.zip3 bounds point worldBackground)
                    (Gen.zip3 objects interactions dynamicWarps)
            |> Gen.map (fun (n, (bs, p, bg), (os, is, dws)) ->
                World.createWithInteractions n bs p bg os is
                |> World.withDynamicWarps dws
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
    static member world (): Arbitrary<World.Payload> = Arb.fromGen SerialisationArbs.genWorld
    static member getJoinGameResult (): Arbitrary<GetJoinGameResult> = Arb.fromGen SerialisationArbs.genGetJoinGameResult
