namespace LuceRPG.Server.Core.WorldGenerator

open NUnit.Framework
open LuceRPG.Models
open FsUnit
open FsCheck

[<TestFixture>]
module Tile =
    [<Test>]
    let ``isRotationOf correct for two matching pieces`` () =
        Arb.register<WorldGeneratorArbs>() |> ignore

        let checkFn (a: Tile) (b: Tile) (turns: int): bool =
            let shouldMatch = a = b
            let rotatedB = Tile.rotateCwN turns b

            a |> Tile.isRotationOf rotatedB = shouldMatch

        Check.QuickThrowOnFailure checkFn

[<TestFixture>]
module TileSet =
    let tileSet =
        let weights: TileSet.FullWeights =
            {
                deadEnd = 1u
                straight = 1u
                L = 1u
                T = 1u
                cross = 1u
            }

        TileSet.full weights

    [<Test>]
    let ``getTile always gets tile with requiredLinks`` () =
        let r = System.Random()

        let checkFn (required: Direction Set): bool =
            let tile = TileSet.getTile r required Set.empty tileSet

            match tile with
            | Option.None -> failwith "No tile returned from set"
            | Option.Some t -> Set.isSuperset t required

        Check.QuickThrowOnFailure checkFn

    [<Test>]
    let ``getTile never gets a tile with disallowedLinks`` () =
        let r = System.Random()

        let checkFn (disallowed: Direction Set): bool =
            let tile = TileSet.getTile r Set.empty disallowed tileSet

            match tile with
            | Option.None -> true
            | Option.Some t -> Set.intersect t disallowed |> Set.isEmpty

        Check.QuickThrowOnFailure checkFn

    [<Test>]
    let ``given same-seed random input always returns same output`` () =
        let checkFn (seed: int): bool =
            let r1 = System.Random(seed)
            let r2 = System.Random(seed)

            let t1 = TileSet.getTile r1 Set.empty Set.empty tileSet
            let t2 = TileSet.getTile r2 Set.empty Set.empty tileSet

            t1 = t2

        Check.QuickThrowOnFailure checkFn
