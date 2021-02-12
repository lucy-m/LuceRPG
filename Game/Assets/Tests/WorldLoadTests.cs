using LuceRPG.Adapters;
using LuceRPG.Game;
using LuceRPG.Game.Models;
using LuceRPG.Game.Overlords;
using LuceRPG.Game.Providers;
using LuceRPG.Game.Services;
using LuceRPG.Game.Utility;
using LuceRPG.Game.WorldObjects;
using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class WorldLoadTests
{
    private TestCommsService testCommsService;
    private TestInputProvider testInputProvider;
    private TestTimestampProvider testTimestampProvider;

    private WithId.Model<WorldObjectModule.Payload> playerModel;
    private WithId.Model<WorldObjectModule.Payload> wallModel;
    private WorldModule.Model world;
    private readonly string clientId = "client-id";
    private readonly string playerName = "test-player";
    private readonly string interactionText = "Hello {player}!";

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Debug.Log("Running set up");

        Registry.Reset();

        testCommsService = new TestCommsService();
        testInputProvider = new TestInputProvider();
        testTimestampProvider = new TestTimestampProvider();

        Registry.Services.Comms = testCommsService;
        Registry.Providers.Input = testInputProvider;
        Registry.Providers.Timestamp = testTimestampProvider;

        SceneManager.LoadScene("GameLoader", LoadSceneMode.Single);
        yield return null;

        playerModel = TestUtil.MakePlayer(4, 8, playerName);

        wallModel =
            WithId.create(WorldObjectModule.create(
                WorldObjectModule.TypeModule.Model.Wall, PointModule.create(2, 4)
            ));

        var wallInteraction = WithId.create(InteractionModule.One.NewChat(interactionText).ToSingletonEnumerable());
        var interactions = InteractionStore.OfInteractions(wallInteraction);

        var worldBounds =
            new RectModule.Model(PointModule.create(0, 0), PointModule.create(10, 10))
            .ToSingletonEnumerable();

        var objects = new WithId.Model<WorldObjectModule.Payload>[]
        {
            playerModel,
            wallModel
        };
        var spawnPoint = PointModule.create(10, 10);

        var wallInteractionMap = Tuple.Create(wallModel.id, wallInteraction.id);
        var interactionMap = new FSharpMap<string, string>(wallInteractionMap.ToSingletonEnumerable());

        world = WorldModule.createWithInteractions(worldBounds, spawnPoint, objects, interactionMap);
        var tsWorld = WithTimestamp.create(0, world);

        var payload = new LoadWorldPayload(clientId, playerModel.id, tsWorld, interactions);
        testCommsService.OnLoad(payload);
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

        yield return null;
    }

    [UnityTest]
    public IEnumerator WorldLoadsCorrectly()
    {
        // Scene correctly loaded
        var worldOverlord = GameObject.FindObjectOfType<WorldOverlord>();
        Assert.That(worldOverlord, Is.Not.Null);

        // Store data is correct
        var worldStore = Registry.Stores.World;
        Assert.That(worldStore.World, Is.EqualTo(world));
        Assert.That(worldStore.PlayerId, Is.EqualTo(playerModel.id));
        Assert.That(worldStore.ClientId, Is.EqualTo(clientId));

        // Player is loaded correctly
        var playerObject = UniversalController.GetById(playerModel.id);
        Assert.That(playerObject, Is.Not.Null);
        var location = playerObject.transform.position;
        var expectedLocation = WorldObjectModule.btmLeft(playerModel).ToVector3();
        Assert.That(location, Is.EqualTo(expectedLocation));

        //Wall loads correctly
        var wallObject = UniversalController.GetById(wallModel.id);
        Assert.That(wallObject, Is.Not.Null);
        location = wallObject.gameObject.transform.position;
        expectedLocation = WorldObjectModule.btmLeft(wallModel).ToVector3();
        Assert.That(location, Is.EqualTo(expectedLocation));

        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayerNameShownCorrectly()
    {
        var unitNameControllers = GameObject.FindObjectsOfType<UnitNameController>();
        Assert.That(unitNameControllers.Length, Is.EqualTo(1));
        Assert.That(unitNameControllers[0].Text.text, Is.EqualTo(playerName));

        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayerReactsToInputsCorrectly()
    {
        var playerObject = UniversalController.GetById(playerModel.id);
        var intentionProcessor = Registry.Processors.Intentions;

        // Pressing down and right
        testInputProvider.VertIn = -1;
        testInputProvider.HorzIn = 1;
        yield return null;

        // Last intention is move down
        Assert.That(testCommsService.LastIntention, Is.Not.Null);
        Assert.That(testCommsService.LastIntention.IsMove, Is.True);
        var moveIntention = (IntentionModule.Type.Move)testCommsService.LastIntention;
        Assert.That(moveIntention.Item1, Is.EqualTo(playerModel.id));
        Assert.That(moveIntention.Item2.IsSouth, Is.True);

        // After player not busy
        var playerBusyUntil = intentionProcessor.BusyUntil(playerModel.id);
        Assert.That(playerBusyUntil.HasValue, Is.True);
        testTimestampProvider.Now = playerBusyUntil.Value;
        yield return null;
        yield return null;

        // Next intention is move right
        Assert.That(testCommsService.LastIntention.IsMove, Is.True);
        moveIntention = (IntentionModule.Type.Move)testCommsService.LastIntention;
        Assert.That(moveIntention.Item1, Is.EqualTo(playerModel.id));
        Assert.That(moveIntention.Item2.IsEast, Is.True);

        Assert.AreEqual(2, testCommsService.AllIntentions.Count);

        // After player not busy
        playerBusyUntil = intentionProcessor.BusyUntil(playerModel.id);
        Assert.That(playerBusyUntil.HasValue, Is.True);
        testTimestampProvider.Now = playerBusyUntil.Value;
        yield return null;
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
        var tsGetSinceResult = WithTimestamp.create(130, getSinceResult);

        testCommsService.OnUpdate(tsGetSinceResult);
        yield return null;

        // Player is not moved, event is ignored
        var postTarget = playerObject.Target;
        Assert.That(postTarget, Is.EqualTo(priorTarget));

        // Store LastUpdated is correct
        Assert.That(Registry.Stores.World.LastUpdate, Is.EqualTo(tsGetSinceResult.timestamp));
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
        var tsGetSinceResult = WithTimestamp.create(testTimestampProvider.Now, getSinceResult);

        testCommsService.OnUpdate(tsGetSinceResult);

        // player object should target to be 1 square north
        var playerObject = UniversalController.GetById(playerModel.id);
        var expectedTarget = playerObject.transform.position + new Vector3(0, 1);
        Assert.That(playerObject.Target, Is.EqualTo(expectedTarget));

        // wall object target should be unchanged
        var wallObject = UniversalController.GetById(wallModel.id);
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
        var tsGetSinceResult = WithTimestamp.create(testTimestampProvider.Now, getSinceResult);

        testCommsService.OnUpdate(tsGetSinceResult);

        // A new player object should be added
        var newPlayerUc = UniversalController.GetById(newPlayerModel.id);
        Assert.That(newPlayerUc, Is.Not.Null);

        // World store is correct
        Assert.That(Registry.Stores.World.PlayerId, Is.EqualTo(playerModel.id));
        var containsPlayer =
            MapModule.ContainsKey(
                newPlayerModel.id,
                Registry.Stores.World.World.objects
            );
        Assert.That(containsPlayer, Is.True);

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
        var tsGetSinceResult = WithTimestamp.create(testTimestampProvider.Now, getSinceResult);

        testCommsService.OnUpdate(tsGetSinceResult);

        yield return null;

        // wall object should be destroyed
        var wallObject = UniversalController.GetById(wallModel.id);
        Assert.That(wallObject == null, Is.True);

        // universal controller should be unregistered
        var uc = UniversalController.GetById(wallModel.id);
        Assert.That(uc, Is.Null);

        // player object is unaffected
        var playerObject = UniversalController.GetById(playerModel.id);
        Assert.That(playerObject != null, Is.True);
    }

    [UnityTest]
    public IEnumerator ClickInteractionsWork()
    {
        // clicking on player
        var playerObject = UniversalController.GetById(playerModel.id);
        playerObject.OnMouseDown();

        // does nothing
        var sbc = GameObject.FindObjectOfType<SpeechBubbleController>();
        Assert.That(sbc, Is.Null);

        // clicking on wall
        var wallObject = UniversalController.GetById(wallModel.id);
        wallObject.OnMouseDown();

        // speech bubble is created correctly
        sbc = GameObject.FindObjectOfType<SpeechBubbleController>();
        Assert.That(sbc, Is.Not.Null);

        var expected = $"Hello {playerName}!";
        Assert.That(sbc.Text.text, Is.EqualTo(expected));

        yield return null;
    }
}
