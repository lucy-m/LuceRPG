namespace LuceRPG.Server.Core.WorldGenerator

open LuceRPG.Models
open LuceRPG.Server.Core

module PathWorld =

    type Model =
        {
            tileMap: Map<Point, Tile>
            externalMap: Map<Point, Direction>
            bounds: Rect
        }

    let create tileMap external bounds: Model =
        {
            tileMap = tileMap
            externalMap = external
            bounds = bounds
        }

    let debugPrint (model: Model): string =
        let xs = [Rect.leftBound model.bounds .. Rect.rightBound model.bounds - 1]
        let ys =
            [Rect.bottomBound model.bounds .. Rect.topBound model.bounds - 1]
            |> List.rev

        ys
        |> List.map (fun y ->
            xs
            |> List.map (fun x ->
                model.tileMap
                |> Map.tryFind (Point.create x y)
                |> Option.map (Tile.asSymbol)
                |> Option.defaultValue ' '
            )
            |> Array.ofList
            |> System.String
            |> fun s -> s + "\n"
        )
        |> List.reduce (+)

    type WithLinks =
        {
            model: Model
            activeLinks: Map<Point, Direction Set>
        }

    let getLinks (model: Model): WithLinks =
        let activeLinks =
            model.tileMap
            |> Map.toSeq
            |> Seq.append (model.externalMap |> Map.toSeq |> Seq.map (fun (p, d) -> p, set[d]))
            |> Seq.map (fun (point, tile) ->
                // Find all directions that point to a missing tile
                let activeDirections =
                    tile
                    |> Set.filter (fun d ->
                        let dp = Direction.movePoint d 1 point
                        let hasTile = model.tileMap |> Map.containsKey dp
                        let hasExternal = model.externalMap |> Map.containsKey dp

                        not hasTile && not hasExternal
                    )

                point, activeDirections
            )
            |> Seq.filter (fun (_, links) -> not (Set.isEmpty links))
            |> Map.ofSeq

        {
            model = model
            activeLinks = activeLinks
        }

    /// Adds a tile to the given point
    /// Ensures all active links for that point will be satisfied
    let addTile
        (random: System.Random)
        (tileSet: TileSet)
        (point: Point)
        (withLinks: WithLinks)
        : WithLinks Option =

        // Directions that this tile must link to
        // Check all neighbouring tiles for an active link
        let requiredLinks =
            Direction.all
            |> Set.filter (fun d ->
                let dp = Direction.movePoint d 1 point
                let tTile = withLinks.activeLinks |> Map.tryFind dp

                match tTile with
                | Option.None ->
                    // No tile exists in given direction
                    false
                | Option.Some tile ->
                    // Check whether the tile requires a link to this tile
                    Set.contains (Direction.inverse d) tile
            )

        // Directions this tile must not link to
        // Existing tiles that do not have a link to this tile
        let blockedLinks =
            Set.difference Direction.all requiredLinks
            |> Set.filter (fun d ->
                let dp = Direction.movePoint d 1 point
                Map.containsKey dp withLinks.model.tileMap
            )

        let tTile = TileSet.getTile random requiredLinks blockedLinks tileSet

        tTile
        |> Option.map (fun tile ->
            let asString = Tile.asString tile

            let tileMap = withLinks.model.tileMap |> Map.add point tile

            let linksToTile =
                tile
                |> Set.map (fun d ->
                    Direction.inverse d, Direction.movePoint d 1 point
                )

            let externalLinks =
                linksToTile
                |> Set.filter (fun (d, dp) ->
                    withLinks.model.bounds
                    |> Rect.contains dp
                    |> not
                )

            let external =
                externalLinks
                |> Seq.fold (fun acc (d, dp) ->

                    acc |> Map.add dp d

                ) withLinks.model.externalMap

            let model =
                {
                    withLinks.model with
                        tileMap = tileMap
                        externalMap = external
                }

            let activeLinks =
                linksToTile
                |> Seq.fold (fun acc (d, dp) ->
                    let tMapEntry = Map.tryFind dp acc

                    match tMapEntry with
                    | Option.Some mapEntry ->
                        // This is an existing entry so is now satisfied
                        let updated = mapEntry |> Set.remove d

                        if Set.isEmpty updated
                        then Map.remove dp acc
                        else Map.add dp updated acc

                    | Option.None ->
                        // This is a new link to a non-existing tile
                        // Add a new active link from this tile if it is internal
                        if Rect.contains dp withLinks.model.bounds
                        then
                            let mapEntry =
                                acc
                                |> Map.tryFind point
                                |> Option.defaultValue Set.empty
                                |> Set.add (Direction.inverse d)

                            Map.add point mapEntry acc
                        else
                            acc

                ) withLinks.activeLinks

            {
                model = model
                activeLinks = activeLinks
            }
        )

    let fill (tileSet: TileSet) (model: Model) (random: System.Random): Model =
        let initial = getLinks model

        let rec fillInner (withLinks: WithLinks): WithLinks =
            let tNextTile =
                withLinks.activeLinks
                |> Map.tryPick (fun p dirs ->
                    Seq.tryHead dirs
                    |> Option.map (fun d -> Direction.movePoint d 1 p)
                )

            tNextTile
            |> Option.bind (fun point ->
                let r = addTile random tileSet point withLinks

                r
            )
            |> Option.map (fun next -> fillInner next)
            |> Option.defaultValue withLinks

        (fillInner initial).model

    /// Gets the tiles in the direction and returns true/false depending on
    ///   whether the map is filled in that direction
    let getDirMostTiles (dir: Direction) (model: Model): bool * (Point * Tile) List =
        if model.tileMap |> Map.isEmpty
        then false, []
        else
            let tiles = model.tileMap |> Map.toList

            match dir with
            | Direction.North ->
                let maxY =
                    tiles
                    |> List.map (fun (p, t) -> p.y)
                    |> List.max
                let bound = (model.bounds |> Rect.topBound) - 1
                maxY = bound, tiles |> List.filter (fun (p, t) -> p.y = maxY)

            | Direction.South ->
                let minY =
                    tiles
                    |> List.map (fun (p, t) -> p.y)
                    |> List.min
                let bound = model.bounds |> Rect.bottomBound
                minY = bound, tiles |> List.filter (fun (p, t) -> p.y = minY)

            | Direction.East ->
                let maxX =
                    tiles
                    |> List.map (fun (p, t) -> p.x)
                    |> List.max
                let bound = (model.bounds |> Rect.rightBound) - 1
                maxX = bound, tiles |> List.filter (fun (p, t) -> p.x = maxX)

            | Direction.West ->
                let minX =
                    tiles
                    |> List.map (fun (p, t) -> p.x)
                    |> List.min
                let bound = model.bounds |> Rect.leftBound
                minX = bound, tiles |> List.filter (fun (p, t) -> p.x = minX)

    /// Fills out the PathWorld in the given direction, ensuring
    ///   the path will reach to the edge
    let fillInDirection
            (tileSet: TileSet)
            (dir: Direction)
            (model: Model)
            (random: System.Random)
            : Model =

        let rec fillInner (model: Model): Model =
            if model.tileMap |> Map.isEmpty
            then model
            else
                let isFilled, candidateTiles = getDirMostTiles dir model

                if isFilled
                then model
                else
                    let tNext = Util.randomOf random candidateTiles

                    match tNext with
                    | Option.Some (point, tile) ->
                        let newTile = tile |> Set.add dir
                        let tileMap = model.tileMap |> Map.add point newTile
                        let newModel = { model with tileMap = tileMap }

                        fill tileSet newModel random
                        |> fillInner
                    | Option.None -> model

        fillInner model

type PathWorld = PathWorld.Model
