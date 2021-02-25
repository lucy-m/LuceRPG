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
        var playerData = CharacterDataModule.create(name);
        var topLeft = PointModule.create(x, y);

        var payload = WorldObjectModule.create(
            WorldObjectModule.TypeModule.Model.NewPlayer(playerData), topLeft);

        return WithId.create(payload);
    }

    public static void AssertXYMatch(Vector3 actual, Vector3 expected)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x));
        Assert.That(actual.y, Is.EqualTo(expected.y));
    }
}
