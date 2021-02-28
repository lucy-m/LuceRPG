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
    }
}
