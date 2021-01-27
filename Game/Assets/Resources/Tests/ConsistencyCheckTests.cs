using LuceRPG.Game.Util;
using LuceRPG.Models;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

public class ConsistencyCheckTests
{
    private TestCommsService testCommsService;
    private TestInputProvider testInputProvider;
    private GameObject overlord;

    private WithId.Model<WorldObjectModule.Payload> modelToMove;
    private WithId.Model<WorldObjectModule.Payload> modelToSnap;
    private WithId.Model<WorldObjectModule.Payload> modelToAdd;
    private WithId.Model<WorldObjectModule.Payload> modelToRemove;

    private WithId.Model<WorldObjectModule.Payload> updatedToMove;
    private WithId.Model<WorldObjectModule.Payload> updatedToSnap;

    private WorldModule.Model updatedWorld;

    private UniversalController objToMove;
    private UniversalController objToSnap;
    private UniversalController objToRemove;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Debug.Log("Running set up");
        testCommsService = new TestCommsService();
        testInputProvider = new TestInputProvider();
        Registry.CommsService = testCommsService;
        Registry.InputProvider = testInputProvider;

        overlord = MonoBehaviour.Instantiate(
            Resources.Load<GameObject>("Prefabs/Overlord")
        );

        Assert.That(overlord, Is.Not.Null);

        yield return null;

        modelToMove =
            WithId.create(
                WorldObjectModule.create(
                    WorldObjectModule.TypeModule.Model.Player, PointModule.create(0, 2)
                )
            );

        modelToSnap =
            WithId.create(
                WorldObjectModule.create(
                    WorldObjectModule.TypeModule.Model.Player, PointModule.create(2, 2)
                )
            );

        modelToAdd =
            WithId.create(
                WorldObjectModule.create(
                    WorldObjectModule.TypeModule.Model.Player, PointModule.create(4, 2)
                )
            );

        modelToRemove =
            WithId.create(
                WorldObjectModule.create(
                    WorldObjectModule.TypeModule.Model.Player, PointModule.create(6, 2)
                )
            );

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

        testCommsService.OnLoad(modelToMove.id, world);

        objToMove = UniversalController.GetById(modelToMove.id);
        objToSnap = UniversalController.GetById(modelToSnap.id);
        objToRemove = UniversalController.GetById(modelToRemove.id);

        updatedToMove = WorldObjectModule.moveObject(DirectionModule.Model.North, 1, modelToMove);
        updatedToSnap = WorldObjectModule.moveObject(DirectionModule.Model.North, 8, modelToSnap);

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

