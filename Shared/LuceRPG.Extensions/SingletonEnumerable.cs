using System.Collections.Generic;

namespace LuceRPG.Utility
{
    public static class SingletonEnumerable
    {
        public static IEnumerable<T> ToSingletonEnumerable<T>(this T t)
        {
            return new List<T>() { t };
        }
    }
}
