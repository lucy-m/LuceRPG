using System;
using System.Collections;
using LuceRPG.Models;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WorldLoadTests
{
    private TestCommsService testCommsService;
    private TestInputProvider testInputProvider;
    private GameObject overlord;
    private WithId.Model<WorldObjectModule.Payload> playerModel;
    private WorldModule.Model world;
    private GameObject playerObject;

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

        playerModel =
            WithId.create(
                WorldObjectModule.create(
                    WorldObjectModule.TypeModule.Model.Player, PointModule.create(4, 8)
                )
            );

        var worldBounds = new RectModule.Model[]
        {
            new RectModule.Model(PointModule.create(0, 10), PointModule.create(10,10))
        };
        var objects = new WithId.Model<WorldObjectModule.Payload>[]
        {
            playerModel,
            WithId.create(WorldObjectModule.create(
                    WorldObjectModule.TypeModule.Model.Wall, PointModule.create(2, 4)
                ))
        };
        var spawnPoint = PointModule.create(10, 10);

        world = WorldModule.createWithObjs(worldBounds, spawnPoint, objects);

        testCommsService.OnLoad(playerModel.id, world);

        playerObject = GameObject.FindGameObjectWithTag("Player");
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

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator PlayerLoadsCorrectly()
    {
        // Player is loaded correctly
        var location = playerObject.transform.position;
        var expectedLocation = CoOrdTranslator.GetGameLocation(playerModel);
        Assert.AreEqual(expectedLocation, location);
        var hasPlayerController = playerObject.TryGetComponent<PlayerController>(out _);
        Assert.True(hasPlayerController);

        var hasUniversalController = playerObject.TryGetComponent<UniversalController>(out var uc);
        Assert.True(hasUniversalController);

        Assert.AreEqual(playerModel.id, uc.Id);

        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayerReactsToInputsCorrectly()
    {
        var pc = playerObject.GetComponent<PlayerController>();

        // Pressing down and right
        testInputProvider.VertIn = -1;
        testInputProvider.HorzIn = 1;
        yield return null;

        // Last intention is move down
        Assert.NotNull(testCommsService.LastIntention);
        Assert.True(testCommsService.LastIntention.IsMove);
        var moveIntention = (IntentionModule.Type.Move)testCommsService.LastIntention;
        Assert.AreEqual(playerModel.id, moveIntention.Item1);
        Assert.True(moveIntention.Item2.IsSouth);

        // After player input delay
        yield return new WaitForSeconds(pc.InputDelay);

        // Next intention is move right
        Assert.True(testCommsService.LastIntention.IsMove);
        moveIntention = (IntentionModule.Type.Move)testCommsService.LastIntention;
        Assert.AreEqual(playerModel.id, moveIntention.Item1);
        Assert.True(moveIntention.Item2.IsEast);

        Assert.AreEqual(2, testCommsService.AllIntentions.Count);
    }

    [UnityTest]
    public IEnumerator PlayerRespondsToUpdateIntention()
    {
        yield return null;
    }
}
