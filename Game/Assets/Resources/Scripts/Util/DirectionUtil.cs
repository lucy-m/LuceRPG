using LuceRPG.Models;
using UnityEngine;

namespace LuceRPG.Game.Util
{
    public static class DirectionUtil
    {
        public static Vector3 AsVector3(DirectionModule.Model dir, byte amount)
        {
            if (dir.IsNorth)
            {
                return new Vector3(0, amount);
            }
            else if (dir.IsSouth)
            {
                return new Vector3(0, -amount);
            }
            else if (dir.IsEast)
            {
                return new Vector3(amount, 0);
            }
            else if (dir.IsWest)
            {
                return new Vector3(-amount, 0);
            }

            return Vector3.zero;
        }
    }
}
