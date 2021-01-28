﻿using LuceRPG.Models;
using UnityEngine;

namespace LuceRPG.Game.Util
{
    public static class DirectionUtil
    {
        public static Vector3 AsVector3(DirectionModule.Model dir)
        {
            if (dir.IsNorth)
            {
                return new Vector3(0, 1);
            }
            else if (dir.IsSouth)
            {
                return new Vector3(0, -1);
            }
            else if (dir.IsEast)
            {
                return new Vector3(1, 0);
            }
            else if (dir.IsWest)
            {
                return new Vector3(-1, 0);
            }

            return Vector3.zero;
        }
    }
}
