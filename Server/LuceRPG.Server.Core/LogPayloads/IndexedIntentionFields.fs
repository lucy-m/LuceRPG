namespace LuceRPG.Server.Core

open LuceRPG.Models

module IndexedIntentionFields =

    let create (ii: IndexedIntention): string seq =
        let id = sprintf "Id %s" ii.tsIntention.value.id
        let index = sprintf "Index %i" ii.index

        seq {id; index}
