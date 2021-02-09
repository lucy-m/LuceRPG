using LuceRPG.Models;
using System;

public static class TestUtil
{
    public static WithId.Model<WorldObjectModule.Payload> MakePlayer(
        int x, int y, string name = null
    )
    {
        name = name ?? Guid.NewGuid().ToString();
        var playerData = PlayerDataModule.create(name);
        var topLeft = PointModule.create(x, y);

        var payload = WorldObjectModule.create(
            WorldObjectModule.TypeModule.Model.NewPlayer(playerData), topLeft);

        return WithId.create(payload);
    }
}
