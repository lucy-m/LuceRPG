namespace LuceRPG.Models

module TestUtil =

    let withGuid (t: 'T): 'T WithGuid =
        let guid = System.Guid.NewGuid()

        WithGuid.create guid t
