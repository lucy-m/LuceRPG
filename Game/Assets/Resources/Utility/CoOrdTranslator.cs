using LuceRPG.Models;
using UnityEngine;

namespace LuceRPG.Game.Utility
{
    public static class CoOrdTranslator
    {
        public static Vector3 GetCenterLocation(this WithId.Model<WorldObjectModule.Payload> obj)
        {
            var size = WorldObjectModule.size(obj.value);
            var btmLeft = WorldObjectModule.btmLeft(obj);

            var location = new Vector3(
                btmLeft.x + size.x * 0.5f,
                btmLeft.y + size.y * 0.5f
            );

            return location;
        }

        public static Vector3 GetCenterLocation(this RectModule.Model rect)
        {
            var location = new Vector3(
                rect.btmLeft.x + rect.size.x * 0.5f,
                rect.btmLeft.y + rect.size.y * 0.5f
            );

            return location;
        }

        public static Vector3 ToVector3(this PointModule.Model p)
        {
            return new Vector3(p.x, p.y);
        }
    }
}
