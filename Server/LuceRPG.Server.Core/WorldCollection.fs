namespace LuceRPG.Server.Core

open LuceRPG.Models

module WorldCollection =
    type Model =
        {
            defaultWorld: Id.World
            allWorlds: (World * Interactions) seq
        }

    let create (defaultWorld: Id.World) (allWorlds: (World * Interactions) seq): Model =
        {
            defaultWorld = defaultWorld
            allWorlds = allWorlds
        }

    let createWithoutInteractions (defaultWorld: Id.World) (withoutInteractions: World seq): Model =
        let withInteractions =
            withoutInteractions
            |> Seq.map (fun w -> (w, List.empty<Interaction>))

        create defaultWorld withInteractions

    let interactions (wc: Model): Interactions =
        wc.allWorlds |> Seq.collect snd |> List.ofSeq

    let interactionMap (wc: Model): Map<string, Interaction> =
        interactions wc
        |> WithId.toMap

type WorldCollection = WorldCollection.Model
