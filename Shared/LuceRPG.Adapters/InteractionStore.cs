using LuceRPG.Models;
using Microsoft.FSharp.Collections;
using System.Collections.Generic;
using System.Linq;

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

        public static InteractionStore OfInteractions(params WithId.Model<IEnumerable<InteractionModule.One>>[] interactions)
        {
            var withFSharpList = interactions.Select(i =>
            {
                var ls = ListModule.OfSeq(i.value);
                return WithId.useId(i.id, ls);
            });

            var map = Empty();

            foreach (var value in withFSharpList)
            {
                map.Value = MapModule.Add(value.id, value, map.Value);
            }

            return map;
        }
    }
}
