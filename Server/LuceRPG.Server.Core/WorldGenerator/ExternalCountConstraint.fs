namespace LuceRPG.Server.Core.WorldGenerator

open LuceRPG.Models
open LuceRPG.Server.Core

module ExternalCountConstraint =
    type Model =
        | Exactly of int
        | Between of int * int // Upper bound is exclusive

    type Result =
        | Satisfied
        | AddExternal
        | RemoveExternal
        | Unsatisfiable

    let externalsInDirection (pathWorld: PathWorld) (dir: Direction): Point Set =
        pathWorld.externalMap
        |> Map.filter (fun p d -> Direction.inverse d = dir)
        |> Map.toSeq
        |> Seq.map fst
        |> Set.ofSeq

    let check
            (pathWorld: PathWorld)
            (direction: Direction)
            (ecc: Model)
            : Result =

        let maxExternals =
            match direction with
            | Direction.North | Direction.South -> pathWorld.bounds.size.x
            | Direction.East | Direction.West -> pathWorld.bounds.size.y

        let satisfiable =
            match ecc with
            | Exactly c ->
                c >= 0 && c <= maxExternals
            | Between (c1, c2) ->
                let upper = System.Math.Max (c1, c2)
                let lower = System.Math.Min (c1, c2)

                upper > 0 && lower <= maxExternals

        if not satisfiable
        then Unsatisfiable
        else
            let externalsCount =
                externalsInDirection pathWorld direction
                |> Set.count

            match ecc with
            | Exactly c ->
                match externalsCount with
                | x when x < c -> AddExternal
                | x when x > c -> RemoveExternal
                | _ -> Satisfied

            | Between (c1, c2) ->
                match externalsCount with
                | x when x < c1 && x < c2 -> AddExternal
                | x when x >= c1 && x >= c2 -> RemoveExternal
                | _ -> Satisfied

    /// Returns Option.None if the constraint is unsatisfiable
    let constrainOnEdge
            (direction: Direction)
            (ecc: Model)
            (random: System.Random)
            (model: PathWorld)
            : PathWorld Option =

        let rec constrainInner (model: PathWorld): PathWorld Option =
            let result = check model direction ecc

            match result with
            | Unsatisfiable -> Option.None
            | Satisfied -> Option.Some model
            | RemoveExternal ->
                // Remove a connection at random by removing the
                //   external and removing the connection from the
                //   neighbouring tile
                let candidateExternals = externalsInDirection model direction

                Util.randomOf random candidateExternals
                |> Option.bind (fun externalToRemove ->
                    let externalMap = model.externalMap |> Map.remove externalToRemove

                    let tileMap =
                        let tileToRemove = Direction.movePoint (Direction.inverse direction) 1 externalToRemove
                        let tNewTile =
                            model.tileMap
                            |> Map.tryFind tileToRemove
                            |> Option.map (fun t -> Set.remove direction t)

                        tNewTile
                        |> Option.map (fun t ->
                            model.tileMap |> Map.add tileToRemove t
                        )
                        |> Option.defaultValue model.tileMap

                    PathWorld.create tileMap externalMap model.bounds
                    |> constrainInner
                )

            | AddExternal ->
                // Pick a random dir-most tile and add an active connection
                //   in the direction. Fill the map using only
                //   straight piece tiles

                // Check that adding in an active link next to an edge and
                //   filling will populate the missing external

                // Only consider tiles without a connection in that direction
                let dirOnlyModel =
                    let tileMap = model.tileMap |> Map.filter (fun p t -> t |> Set.contains direction |> not)
                    PathWorld.create tileMap model.externalMap model.bounds

                let isFilled, candidateTiles = PathWorld.getDirMostTiles direction dirOnlyModel

                Util.randomOf random candidateTiles
                |> Option.bind (fun (point, tile) ->
                    let tileMap =
                        let newTile = tile |> Set.add direction
                        model.tileMap |> Map.add point newTile

                    // If this map is filled to the edge then add an entry to the tile
                    //   and an external
                    if isFilled
                    then
                        let externalMap =
                            let externalPoint = Direction.movePoint direction 1 point
                            model.externalMap |> Map.add externalPoint (Direction.inverse direction)

                        PathWorld.create tileMap externalMap model.bounds
                        |> constrainInner

                    else
                        // Need to add the link to the tile and run the fill algorithm
                        // with only a straight piece

                        let withActiveLink = PathWorld.create tileMap model.externalMap model.bounds

                        let fillTileSet =
                            match direction with
                            | Direction.North | Direction.South -> Tile.straightNS
                            | Direction.East | Direction.West -> Tile.straightEW
                            |> fun t -> t, 1u
                            |> List.singleton

                        PathWorld.fill fillTileSet random withActiveLink
                        |> constrainInner
                )

        constrainInner model

    /// Applies all the given constraints
    /// Unsatisfiable constraints are ignored
    let constrainAll
            (eccs: Map<Direction, Model>)
            (random: System.Random)
            (model: PathWorld)
            : PathWorld =

        eccs
        |> Map.fold (fun acc d ecc ->
            constrainOnEdge d ecc random acc
            |> Option.defaultValue acc
        ) model

type ExternalCountConstraint = ExternalCountConstraint.Model
