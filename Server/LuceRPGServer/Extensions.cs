using Microsoft.FSharp.Core;

namespace LuceRPG.Server
{
    public static class Extensions
    {
        public static bool HasValue<T>(this FSharpOption<T> option)
        {
            return FSharpOption<T>.get_IsSome(option);
        }
    }
}
