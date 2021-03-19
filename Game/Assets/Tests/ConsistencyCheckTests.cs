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

    private WorldModule.Payload updatedWorld;

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

        modelToMove = TestUtil.MakePlayer(0, 0);
        modelToSnap = TestUtil.MakePlayer(2, 0);
        modelToAdd = TestUtil.MakePlayer(4, 0);
        modelToRemove = TestUtil.MakePlayer(6, 0);

        var worldBounds = new RectModule.Model[]
        {
            new RectModule.Model(PointModule.create(0, 0), PointModule.create(10,10))
        };
        var objects = new WithId.Model<WorldObjectModule.Payload>[]
        {
            modelToMove,
            modelToSnap,
            modelToRemove
        };
        var spawnPoint = PointModule.create(10, 10);

        var world = WorldModule.createWithObjs(
            "test", worldBounds, spawnPoint, WorldBackgroundModule.GreenGrass, objects
        );
        var idWorld = WithId.create(world);
        var tsWorld = WithTimestamp.create(0, idWorld);
        var interactions = InteractionStore.Empty();

        var payload = new LoadWorldPayload("", "", tsWorld, interactions);
        testCommsService.OnLoad(payload);

        updatedToMove = WithId.useId(
            modelToMove.id,
            WorldObjectModule.moveObject(DirectionModule.Model.North, modelToMove.value)
        );

        updatedToSnap = WithId.useId(
            modelToSnap.id,
            WorldObjectModule.moveObjectN(DirectionModule.Model.North, 8, modelToSnap.value)
        );

        var updatedObjects = new WithId.Model<WorldObjectModule.Payload>[]
        {
            updatedToMove,
            updatedToSnap,
            modelToAdd
        };

        updatedWorld = WorldModule.createWithObjs(
            "test", worldBounds, spawnPoint, WorldBackgroundModule.GreenGrass, updatedObjects
        );
    }

    private static void ObjMatchesModelPosition(
        UniversalController go,
        WithId.Model<WorldObjectModule.Payload> wo)
    {
        var actual = go.transform.position;
        var expected = wo.GetBtmLeft();

        TestUtil.AssertXYMatch(actual, expected);
    }

    [UnityTest]
    public IEnumerator WorldSetUpCorrectly()
    {
        var objToMove = UniversalController.GetById(modelToMove.id);
        var objToSnap = UniversalController.GetById(modelToSnap.id);
        var objToRemove = UniversalController.GetById(modelToRemove.id);

        ObjMatchesModelPosition(objToMove, modelToMove);
        ObjMatchesModelPosition(objToSnap, modelToSnap);
        ObjMatchesModelPosition(objToRemove, modelToRemove);

        Assert.That(UniversalController.GetById(modelToAdd.id), Is.Null);

        yield return null;
    }

    [UnityTest]
    public IEnumerator MoveObjectCorrect()
    {
        testCommsService.OnConsistencyCheck(updatedWorld);

        // should not move immediately
        var objToMove = UniversalController.GetById(modelToMove.id);
        ObjMatchesModelPosition(objToMove, modelToMove);

        // target should be set to the new location
        TestUtil.AssertXYMatch(objToMove.Target, updatedToMove.GetBtmLeft());

        yield return null;
    }

    [UnityTest]
    public IEnumerator SnapObjectCorrect()
    {
        testCommsService.OnConsistencyCheck(updatedWorld);

        // moves position and target to the new location
        var objToSnap = UniversalController.GetById(modelToSnap.id);
        ObjMatchesModelPosition(objToSnap, updatedToSnap);
        TestUtil.AssertXYMatch(objToSnap.Target, updatedToSnap.GetBtmLeft());

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
        ObjMatchesModelPosition(newObj, modelToAdd);

        yield return null;
    }
}
