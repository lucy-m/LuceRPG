namespace LuceRPG.Server.Core

module Util =
    let randomOf (random: System.Random) (ls: 'a seq): 'a Option =
        let len = Seq.length ls

        if len = 0
        then Option.None
        else
            let n = random.Next(Seq.length ls)

            ls |> Seq.skip n |> Seq.head |> Option.Some
