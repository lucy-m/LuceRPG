using System.Collections;
using System.Collections.Generic;
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
        var overlord = MonoBehaviour.Instantiate(
            Resources.Load<GameObject>("Prefabs/Overlord")
        );
        Assert.IsNotNull(WorldLoader.Instance);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator NewTestScriptWithEnumeratorPasses()
    {
        var overlord = MonoBehaviour.Instantiate(
            Resources.Load<GameObject>("Prefabs/Overlord")
        );
        var worldLoader = overlord.GetComponent<WorldLoader>();
        Assert.IsNotNull(overlord);
        Assert.IsNotNull(worldLoader);

        var world = LuceRPG.Samples.SampleWorlds.world1;

        worldLoader.LoadWorld("playerId", world);

        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return new WaitForSeconds(1);
    }
}
