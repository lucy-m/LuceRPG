using LuceRPG.Models;
using Microsoft.FSharp.Collections;

namespace LuceRPG.Adapters
{
    public class InteractionStore
    {
        public FSharpMap<string, WithId.Model<FSharpList<InteractionModule.One>>>
            Value
        { get; set; }

        public InteractionStore(
            FSharpMap<string, WithId.Model<FSharpList<InteractionModule.One>>> value)
        {
            Value = value;
        }

        public static InteractionStore Empty()
        {
            return new InteractionStore(
                MapModule.Empty<string, WithId.Model<FSharpList<InteractionModule.One>>>()
            );
        }
    }
}
