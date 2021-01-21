using Microsoft.FSharp.Core;
using System;

namespace LuceRPG.Utility
{
    public static class FSharpExtensions
    {
        public static bool HasValue<T>(this FSharpOption<T> option)
        {
            return FSharpOption<T>.get_IsSome(option);
        }

        public static FSharpFunc<TFrom, TTo> ToFSharpFunc<TFrom, TTo>(this Func<TFrom, TTo> fn)
        {
            return FSharpFunc<TFrom, TTo>.FromConverter(a => fn(a));
        }
    }
}
