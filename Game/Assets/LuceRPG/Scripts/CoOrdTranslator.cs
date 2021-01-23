using LuceRPG.Models;
using UnityEngine;

public static class CoOrdTranslator
{
    public static Vector3 GetGameLocation(this WithId.Model<WorldObjectModule.Payload> obj)
    {
        var size = WorldObjectModule.size(obj);
        var topLeft = WorldObjectModule.topLeft(obj);

        var location = new Vector3(
            topLeft.x + size.x * 0.5f,
            topLeft.y - size.y * 0.5f
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
