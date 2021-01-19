namespace LuceRPG.Serialisation

open LuceRPG.Models
open FsCheck

type SerialisationArbs() =
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
        let topLeft =
            Gen.choose (-100,100)
            |> Gen.two
            |> Gen.map (fun (x,y) -> Point.create x y)

        let objType =
            Arb.generate<WorldObject.Type>

        Gen.zip objType topLeft
        |> Gen.map (fun (t, p) -> WorldObject.create t p)

    static member genWorld: Gen<World> =
        let bounds = Gen.listOf Arb.generate<Rect>
        let objects = Gen.listOf Arb.generate<WorldObject>

        let world =
            Gen.zip bounds objects
            |> Gen.map (fun (bs, os) ->
                World.createWithObjs bs os
            )

        world

    static member rect (): Arbitrary<Rect> = Arb.fromGen SerialisationArbs.genRect
    static member worldObject (): Arbitrary<WorldObject> = Arb.fromGen SerialisationArbs.genWorldObject
    static member world (): Arbitrary<World> = Arb.fromGen SerialisationArbs.genWorld
