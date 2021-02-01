using LuceRPG.Game.Util;
using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

public class WorldLoadTests
{
    private TestCommsService testCommsService;
    private TestInputProvider testInputProvider;
    private TestTimestampProvider testTimestampProvider;
    private GameObject overlord;
    private WithId.Model<WorldObjectModule.Payload> playerModel;
    private WithId.Model<WorldObjectModule.Payload> wallModel;
    private WorldModule.Model world;
    private UniversalController playerObject;
    private UniversalController wallObject;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Debug.Log("Running set up");
        testCommsService = new TestCommsService();
        testInputProvider = new TestInputProvider();
        testTimestampProvider = new TestTimestampProvider();
        Registry.CommsService = testCommsService;
        Registry.InputProvider = testInputProvider;
        Registry.TimestampProvider = testTimestampProvider;

        overlord = MonoBehaviour.Instantiate(
            Resources.Load<GameObject>("Prefabs/Overlord")
        );

        Assert.That(overlord, Is.Not.Null);

        yield return null;

        playerModel = TestUtil.MakePlayer(4, 8);

        wallModel =
            WithId.create(WorldObjectModule.create(
                WorldObjectModule.TypeModule.Model.Wall, PointModule.create(2, 4)
            ));

        var worldBounds = new RectModule.Model[]
        {
            new RectModule.Model(PointModule.create(0, 10), PointModule.create(10,10))
        };
        var objects = new WithId.Model<WorldObjectModule.Payload>[]
        {
            playerModel,
            wallModel
        };
        var spawnPoint = PointModule.create(10, 10);

        world = WorldModule.createWithObjs(worldBounds, spawnPoint, objects);

        testCommsService.OnLoad(playerModel.id, world);

        playerObject = UniversalController.GetById(playerModel.id);
        wallObject = UniversalController.GetById(wallModel.id);
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
    public IEnumerator WorldLoadsCorrectly()
    {
        // Player is loaded correctly
        Assert.That(playerObject, Is.Not.Null);
        var location = playerObject.transform.position;
        var expectedLocation = playerModel.GetGameLocation();
        Assert.That(location, Is.EqualTo(expectedLocation));

        var hasPlayerController = playerObject.TryGetComponent<PlayerController>(out _);
        Assert.That(hasPlayerController, Is.True);

        //Wall loads correctly
        Assert.That(wallObject, Is.Not.Null);
        location = wallObject.gameObject.transform.position;
        expectedLocation = wallModel.GetGameLocation();
        Assert.That(location, Is.EqualTo(expectedLocation));

        hasPlayerController = wallObject.TryGetComponent<PlayerController>(out _);
        Assert.That(hasPlayerController, Is.False);

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

        // After player not busy
        var playerBusyUntil = OptimisticIntentionProcessor.Instance.BusyUntil(playerModel.id);
        Assert.That(playerBusyUntil.HasValue, Is.True);
        testTimestampProvider.Now = playerBusyUntil.Value;
        yield return null;

        // Next intention is move right
        Assert.True(testCommsService.LastIntention.IsMove);
        moveIntention = (IntentionModule.Type.Move)testCommsService.LastIntention;
        Assert.AreEqual(playerModel.id, moveIntention.Item1);
        Assert.True(moveIntention.Item2.IsEast);

        Assert.AreEqual(2, testCommsService.AllIntentions.Count);

        // After player not busy
        playerBusyUntil = OptimisticIntentionProcessor.Instance.BusyUntil(playerModel.id);
        Assert.That(playerBusyUntil.HasValue, Is.True);
        testTimestampProvider.Now = playerBusyUntil.Value;
        yield return null;

        // Inputs removed
        testInputProvider.HorzIn = 0;
        testInputProvider.VertIn = 0;

        var priorTarget = playerObject.Target;

        // Event returned from server with same id
        var serverEventT = WorldEventModule.Type.NewMoved(playerModel.id, DirectionModule.Model.East);
        var serverEvent = WorldEventModule.asResult(testCommsService.LastIntentionId, 0, serverEventT);
        var tsEvent = WithTimestamp.create(testTimestampProvider.Now, serverEvent);
        var eventList = ListModule.OfSeq(new WithTimestamp.Model<WorldEventModule.Model>[] { tsEvent });
        var getSinceResult = GetSinceResultModule.Payload.NewEvents(eventList);

        testCommsService.OnUpdate(getSinceResult);
        yield return null;

        // Player is not moved, event is ignored
        var postTarget = playerObject.Target;
        Assert.That(postTarget, Is.EqualTo(priorTarget));
    }

    [UnityTest]
    public IEnumerator PlayerRespondsToUpdateIntention()
    {
        var worldEvents = new List<WithTimestamp.Model<WorldEventModule.Model>>
        {
            WithTimestamp.create(1,
                new WorldEventModule.Model(
                    "intention1",
                    0,
                    WorldEventModule.Type.NewMoved(playerModel.id, DirectionModule.Model.North)
                )
            )
        };
        var getSinceResult =
                GetSinceResultModule.Payload.NewEvents(ListModule.OfSeq(worldEvents));

        testCommsService.OnUpdate(getSinceResult);

        // player object should target to be 1 square north
        var expectedTarget = playerObject.transform.position + new Vector3(0, 1);
        Assert.That(playerObject.Target, Is.EqualTo(expectedTarget));

        // wall object target should be unchanged
        var wallUc = wallObject.GetComponent<UniversalController>();
        expectedTarget = wallObject.transform.position;
        Assert.That(wallUc.Target, Is.EqualTo(expectedTarget));

        // after a frame the player moves towards the target
        var priorPosition = playerObject.transform.position;
        yield return null;
        var afterPosition = playerObject.transform.position;
        var positionChange = afterPosition - priorPosition;

        var movesNorth = Vector3.Dot(positionChange.normalized, new Vector3(0, 1)) == 1;

        Assert.That(movesNorth, Is.True);

        // the player eventually reaches the target
        for (var i = 0; i < 10; i++)
        {
            if (playerObject.transform.position == playerObject.Target)
            {
                break;
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        Assert.That(playerObject.transform.position, Is.EqualTo(playerObject.Target));
    }

    [UnityTest]
    public IEnumerator AddObjectWorldEventCorrectlyHandled()
    {
        var newPlayerModel = TestUtil.MakePlayer(5, 4);

        var worldEvents = new List<WithTimestamp.Model<WorldEventModule.Model>>
        {
            WithTimestamp.create(1,
                new WorldEventModule.Model(
                        "intention1",
                        0,
                        WorldEventModule.Type.NewObjectAdded(newPlayerModel)
                )
            )
        };
        var getSinceResult =
            GetSinceResultModule.Payload.NewEvents(ListModule.OfSeq(worldEvents));

        testCommsService.OnUpdate(getSinceResult);

        // A new player object should be added without player controller
        var newPlayerUc = UniversalController.GetById(newPlayerModel.id);
        Assert.That(newPlayerUc, Is.Not.Null);

        var newPlayerHasPc = newPlayerUc.TryGetComponent<PlayerController>(out _);
        Assert.That(newPlayerHasPc, Is.False);

        yield return null;
    }

    [UnityTest]
    public IEnumerator RemoveObjectWorldEventCorrectlyHandled()
    {
        var worldEvents = new List<WithTimestamp.Model<WorldEventModule.Model>>
        {
            WithTimestamp.create(1,
                new WorldEventModule.Model(
                        "intention1",
                        0,
                        WorldEventModule.Type.NewObjectRemoved(wallModel.id)
                )
            )
        };
        var getSinceResult =
            GetSinceResultModule.Payload.NewEvents(ListModule.OfSeq(worldEvents));

        testCommsService.OnUpdate(getSinceResult);

        yield return null;

        // wall object should be destroyed
        Assert.That(wallObject == null, Is.True);

        // universal controller should be unregistered
        var uc = UniversalController.GetById(wallModel.id);
        Assert.That(uc, Is.Null);

        // player object is unaffected
        Assert.That(playerObject != null, Is.True);
    }
}
