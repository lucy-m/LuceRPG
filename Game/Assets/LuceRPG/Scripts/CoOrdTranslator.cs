using LuceRPG.Models;
using UnityEngine;

public static class CoOrdTranslator
{
    public static Vector3 GetGameLocation(this WorldObjectModule.Model obj)
    {
        var size = WorldObjectModule.size(obj);

        var location = new Vector3(
            obj.topLeft.x + size.x * 0.5f,
            obj.topLeft.y - size.y * 0.5f
        );

        return location;
    }

    public static Vector3 GetGameLocation(this RectModule.Model rect)
    {
        var location = new Vector3(
            rect.topLeft.x + rect.size.x * 0.5f,
            rect.topLeft.y - rect.size.y * 0.5f
        );

        return location;
    }
}
