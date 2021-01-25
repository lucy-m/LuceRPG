using System.Collections;
using LuceRPG.Models;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class NewTestScript
{
    // A Test behaves as an ordinary method
    [Test]
    public void NewTestScriptSimplePasses()
    {
        MonoBehaviour.Instantiate(
            Resources.Load<GameObject>("Prefabs/Overlord")
        );
        Assert.IsNotNull(WorldLoader.Instance);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator NewTestScriptWithEnumeratorPasses()
    {
        var testCommsService = new TestCommsService();
        Registry.CommsService = testCommsService;

        var overlord = MonoBehaviour.Instantiate(
            Resources.Load<GameObject>("Prefabs/Overlord")
        );
        Assert.IsNotNull(overlord);

        yield return null;

        var playerId = "player";
        var worldBounds = new RectModule.Model[]
        {
            new RectModule.Model(PointModule.create(0, 10), PointModule.create(10,10))
        };
        var objects = new WithId.Model<WorldObjectModule.Payload>[]
        {
            WithId.useId(playerId,
                WorldObjectModule.create(
                    WorldObjectModule.TypeModule.Model.Player, PointModule.create(4, 8)
                )
            ),
            WithId.create(WorldObjectModule.create(
                    WorldObjectModule.TypeModule.Model.Wall, PointModule.create(2, 4)
                ))
        };
        var spawnPoint = PointModule.create(10, 10);

        var world = WorldModule.createWithObjs(worldBounds, spawnPoint, objects);

        testCommsService.OnLoad("player", world);

        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return new WaitForSeconds(1);
    }
}
