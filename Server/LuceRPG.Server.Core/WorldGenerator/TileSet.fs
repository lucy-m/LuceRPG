namespace LuceRPG.Server.Core.WorldGenerator

open LuceRPG.Models

module Tile =

    type Model = Direction Set

    let rotateCw (tile: Model): Model =
        tile
        |> Set.map Direction.rotateCw

    let rotateCwN (turns: int) (tile: Model): Model =
        tile
        |> Set.map (Direction.rotateCwN turns)

    let isRotationOf (a: Model) (b: Model): bool =
        [0 .. 3]
        |> List.map (fun turns -> rotateCwN turns a)
        |> List.map (fun a -> a = b)
        |> List.reduce (||)

    let empty: Model = Set.empty
    let deadEnd: Model = set [Direction.North]
    let straight: Model = set [Direction.North; Direction.South]
    let L: Model = set [Direction.North; Direction.East]
    let T: Model = set [Direction.North; Direction.East; Direction.South]
    let cross: Model = set [Direction.North; Direction.East; Direction.South; Direction.West]

    let deadEndN = deadEnd
    let deadEndE = rotateCw deadEndN
    let deadEndS = rotateCw deadEndE
    let deadEndW = rotateCw deadEndS

    let straightNS = straight
    let straightEW = rotateCw straightNS

    let LNE = L
    let LES = rotateCw LNE
    let LSW = rotateCw LES
    let LWN = rotateCw LSW

    let TNES = T
    let TESW = rotateCw TNES
    let TSWN = rotateCw TESW
    let TWNE = rotateCw TSWN

    let asString (tile: Model): string =
        tile
        |> Set.map Direction.asLetter
        |> Seq.toArray
        |> System.String

    let asSymbol (tile: Model): char =
        match tile with
        | x when x = deadEndN -> '╹'
        | x when x = deadEndE -> '╺'
        | x when x = deadEndS -> '╻'
        | x when x = deadEndW -> '╸'

        | x when x = straightNS -> '┃'
        | x when x = straightEW -> '━'

        | x when x = LNE -> '┗'
        | x when x = LES -> '┏'
        | x when x = LSW -> '┓'
        | x when x = LWN -> '┛'

        | x when x = TNES -> '┣'
        | x when x = TESW -> '┳'
        | x when x = TSWN -> '┫'
        | x when x = TWNE -> '┻'

        | x when x = cross -> '╋'

        | _ -> '█'

type Tile = Tile.Model

module TileSet =

    type Model = (Tile * uint) List

    let addTile
            (tile: Tile)
            (weight: uint)
            (allowedRotations: int Set)
            (model: Model)
            : Model =

        let newTiles =
            allowedRotations
            |> Seq.map (fun r -> Tile.rotateCwN r tile)
            |> Seq.map (fun t -> (t, weight))
            |> Seq.toList

        List.append model newTiles

    let create (tiles: (Tile * uint * int Set) seq): Model =
        tiles
        |> Seq.fold (fun acc (tile, weight, rotations) ->
            addTile tile weight rotations acc
        ) []

    let getTile
            (random: System.Random)
            (requiredLinks: Direction Set)
            (disallowedLinks: Direction Set)
            (model: Model)
            : Tile Option =

        let allowedTiles =
            model
            |> List.filter (fun (t, _) -> Set.isSuperset t requiredLinks)
            |> List.filter (fun (t, _) -> Set.intersect t disallowedLinks |> Set.isEmpty)

        if allowedTiles |> List.isEmpty
        then Option.None
        else

            let maxWeight =
                allowedTiles
                |> Seq.map snd
                |> Seq.reduce (+)

            let n = random.Next((int)maxWeight)

            // Find the first value whose accumulated weight is > n
            // e.g. for weights 0 1 2 3 4
            //              acc 0 1 3 6 10
            //      n = 0 -> 1
            //      n = 5 -> 3

            let tile =
                let rec calcTile (accWeight: int) (tiles: Model): Tile Option =
                    match tiles with
                    | [tile, _] -> Option.Some tile
                    | (tile, w)::tl ->
                        let newAccWeight = accWeight + (int) w
                        if newAccWeight > n
                        then Option.Some tile
                        else calcTile newAccWeight tl
                    | _ -> Option.None

                calcTile 0 allowedTiles

            tile

    type FullWeights =
        {
            deadEnd: uint
            straight: uint
            L: uint
            T: uint
            cross: uint
        }

    let full (weights: FullWeights): Model =
        let allRotations = set [0 .. 3]

        []
        |> addTile Tile.deadEnd weights.deadEnd allRotations
        |> addTile Tile.straight weights.straight (set [0; 2])
        |> addTile Tile.L weights.L allRotations
        |> addTile Tile.T weights.T allRotations
        |> addTile Tile.cross weights.cross (set [0])

    let fullUniform: Model =
        let weights =
            {
                deadEnd = 1u
                straight = 1u
                L = 1u
                T = 1u
                cross = 1u
            }

        full weights

type TileSet = TileSet.Model
