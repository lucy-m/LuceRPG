using System;
using UnityEngine;

namespace LuceRPG.Game.Utility
{
    public static class ColourTranslator
    {
        public static Color ToColor(this Tuple<byte, byte, byte> bytes)
        {
            var r = bytes.Item1 / 255.0f;
            var b = bytes.Item2 / 255.0f;
            var g = bytes.Item3 / 255.0f;

            return new Color(r, b, g);
        }

        public static Tuple<byte, byte, byte> ToByteTuple(this Color c)
        {
            var r = (byte)(c.r * 255);
            var g = (byte)(c.g * 255);
            var b = (byte)(c.b * 255);

            return Tuple.Create(r, g, b);
        }
    }
}
