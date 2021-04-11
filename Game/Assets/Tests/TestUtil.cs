using LuceRPG.Models;
using NUnit.Framework;
using System;
using UnityEngine;

public static class TestUtil
{
    public static WithId.Model<WorldObjectModule.Payload> MakePlayer(
        int x, int y, string name = null
    )
    {
        name ??= Guid.NewGuid().ToString();
        var playerData = CharacterDataModule.randomized(name);
        var topLeft = PointModule.create(x, y);

        var payload = WorldObjectModule.create(
            WorldObjectModule.TypeModule.Model.NewPlayer(playerData), topLeft, DirectionModule.Model.South);

        return WithId.create(payload);
    }
}
