namespace LuceRPG.Serialisation

open LuceRPG.Models

module DirectionSrl =
    let serialise (d: Direction): byte[] =
        let b =
            match d with
            | Direction.North -> 1uy
            | Direction.South -> 2uy
            | Direction.East -> 3uy
            | Direction.West -> 4uy

        [|b|]

    let deserialise (bytes: byte[]): Direction DesrlResult =
        let mapFn (b: byte): Direction Option =
            match b with
            | 1uy -> Option.Some Direction.North
            | 2uy -> Option.Some Direction.South
            | 3uy -> Option.Some Direction.East
            | 4uy -> Option.Some Direction.West
            | _ -> Option.None

        let byte = ByteSrl.deserialise bytes

        DesrlResult.bind mapFn byte
