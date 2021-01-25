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

        var world = LuceRPG.Samples.SampleWorlds.world1;
        var player = WorldObjectModule.create(WorldObjectModule.TypeModule.Model.Player, PointModule.create(4, 2));

        testCommsService.OnLoad("player", LuceRPG.Samples.SampleWorlds.world1);

        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return new WaitForSeconds(1);
    }
}
