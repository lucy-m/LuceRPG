namespace LuceRPG.Models

module TestUtil =

    let withId (t: 'T): 'T WithId =
        let guid = System.Guid.NewGuid().ToString()

        WithId.useId guid t
