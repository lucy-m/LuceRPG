namespace LuceRPG.Serialisation

module Util =
    let safeSkip (count: int) (ts: 'T[]): 'T[] =
        if ts.Length <= count
        then [||]
        else Array.skip count ts
