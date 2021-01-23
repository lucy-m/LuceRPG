namespace LuceRPG.Serialisation

open LuceRPG.Models
open FsCheck

type SerialisationArbs() =
    static member genString: Gen<string> =
        Arb.generate<string>
        |> Gen.map (fun s -> if s = null then "" else s)

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
        |> Gen.map (fun (id, t, p) -> WorldObject.create t p |> WithId.create id)

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

        let world =
            Gen.zip3 bounds point objects
            |> Gen.map (fun (bs, p, os) ->
                World.createWithObjs bs p os
            )

        world

    static member string (): Arbitrary<string> = Arb.fromGen SerialisationArbs.genString
    static member rect (): Arbitrary<Rect> = Arb.fromGen SerialisationArbs.genRect
    static member worldObject (): Arbitrary<WorldObject> = Arb.fromGen SerialisationArbs.genWorldObject
    static member world (): Arbitrary<World> = Arb.fromGen SerialisationArbs.genWorld
