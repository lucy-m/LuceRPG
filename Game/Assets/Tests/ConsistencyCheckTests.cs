using LuceRPG.Adapters;
using LuceRPG.Game;
using LuceRPG.Game.Models;
using LuceRPG.Game.Providers;
using LuceRPG.Game.Services;
using LuceRPG.Game.Utility;
using LuceRPG.Game.WorldObjects;
using LuceRPG.Models;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ConsistencyCheckTests
{
    private TestCommsService testCommsService;
    private TestInputProvider testInputProvider;

    private WithId.Model<WorldObjectModule.Payload> modelToMove;
    private WithId.Model<WorldObjectModule.Payload> modelToSnap;
    private WithId.Model<WorldObjectModule.Payload> modelToAdd;
    private WithId.Model<WorldObjectModule.Payload> modelToRemove;

    private WithId.Model<WorldObjectModule.Payload> updatedToMove;
    private WithId.Model<WorldObjectModule.Payload> updatedToSnap;

    private WorldModule.Model updatedWorld;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Debug.Log("Running set up");
        Registry.Reset();
        testCommsService = new TestCommsService();
        testInputProvider = new TestInputProvider();
        Registry.Services.Comms = testCommsService;
        Registry.Providers.Input = testInputProvider;

        SceneManager.LoadScene("GameLoader", LoadSceneMode.Single);
        yield return null;

        modelToMove = TestUtil.MakePlayer(0, 2);
        modelToSnap = TestUtil.MakePlayer(2, 2);
        modelToAdd = TestUtil.MakePlayer(4, 2);
        modelToRemove = TestUtil.MakePlayer(6, 2);

        var worldBounds = new RectModule.Model[]
        {
            new RectModule.Model(PointModule.create(0, 10), PointModule.create(10,10))
        };
        var objects = new WithId.Model<WorldObjectModule.Payload>[]
        {
            modelToMove,
            modelToSnap,
            modelToRemove
        };
        var spawnPoint = PointModule.create(10, 10);

        var world = WorldModule.createWithObjs(worldBounds, spawnPoint, objects);
        var tsWorld = WithTimestamp.create(0, world);
        var interactions = InteractionStore.Empty();

        var payload = new LoadWorldPayload("", "", tsWorld, interactions);
        testCommsService.OnLoad(payload);

        updatedToMove = WorldObjectModule.moveObject(DirectionModule.Model.North, modelToMove);
        updatedToSnap = WorldObjectModule.moveObjectN(DirectionModule.Model.North, 8, modelToSnap);

        var updatedObjects = new WithId.Model<WorldObjectModule.Payload>[]
        {
            updatedToMove,
            updatedToSnap,
            modelToAdd
        };

        updatedWorld = WorldModule.createWithObjs(worldBounds, spawnPoint, updatedObjects);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Debug.Log("Tearing down");
        var objects = GameObject.FindObjectsOfType<GameObject>();
        foreach (var o in objects)
        {
            MonoBehaviour.Destroy(o);
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator WorldSetUpCorrectly()
    {
        var objToMove = UniversalController.GetById(modelToMove.id);
        var objToSnap = UniversalController.GetById(modelToSnap.id);
        var objToRemove = UniversalController.GetById(modelToRemove.id);

        Assert.That(objToMove.transform.position, Is.EqualTo(modelToMove.GetGameLocation()));
        Assert.That(objToSnap.transform.position, Is.EqualTo(modelToSnap.GetGameLocation()));
        Assert.That(objToRemove.transform.position, Is.EqualTo(modelToRemove.GetGameLocation()));
        Assert.That(UniversalController.GetById(modelToAdd.id), Is.Null);

        yield return null;
    }

    [UnityTest]
    public IEnumerator MoveObjectCorrect()
    {
        testCommsService.OnConsistencyCheck(updatedWorld);

        // should not move immediately
        var objToMove = UniversalController.GetById(modelToMove.id);
        Assert.That(objToMove.transform.position, Is.EqualTo(modelToMove.GetGameLocation()));

        // target should be set to the new location
        Assert.That(objToMove.Target, Is.EqualTo(updatedToMove.GetGameLocation()));

        yield return null;
    }

    [UnityTest]
    public IEnumerator SnapObjectCorrect()
    {
        testCommsService.OnConsistencyCheck(updatedWorld);

        // moves position and target to the new location
        var objToSnap = UniversalController.GetById(modelToSnap.id);
        Assert.That(objToSnap.transform.position, Is.EqualTo(updatedToSnap.GetGameLocation()));
        Assert.That(objToSnap.Target, Is.EqualTo(updatedToSnap.GetGameLocation()));

        yield return null;
    }

    [UnityTest]
    public IEnumerator RemoveObjectCorrect()
    {
        testCommsService.OnConsistencyCheck(updatedWorld);

        // GameObjects are deleted at the end of a frame
        yield return null;

        // obj should be null
        var objToRemove = UniversalController.GetById(modelToRemove.id);
        Assert.That(objToRemove == null, Is.True);

        yield return null;
    }

    [UnityTest]
    public IEnumerator AddObjectCorrect()
    {
        testCommsService.OnConsistencyCheck(updatedWorld);

        // obj should be created
        var newObj = UniversalController.GetById(modelToAdd.id);
        Assert.That(newObj == null, Is.False);

        Assert.That(newObj.transform.position, Is.EqualTo(modelToAdd.GetGameLocation()));

        yield return null;
    }
}
