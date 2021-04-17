namespace LuceRPG.Server.Core.WorldGenerator

open FsCheck

type WorldGeneratorArbs () =
    static member genTile: Gen<Tile> =
        Gen.elements [Tile.deadEnd; Tile.straight; Tile.L; Tile.T; Tile.cross]

    static member tile (): Arbitrary<Tile> = Arb.fromGen WorldGeneratorArbs.genTile
