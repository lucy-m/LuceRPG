﻿using LuceRPG.Adapters;
using LuceRPG.Game;
using LuceRPG.Game.Models;
using LuceRPG.Game.Overlords;
using LuceRPG.Game.Providers;
using LuceRPG.Game.Services;
using LuceRPG.Game.Stores;
using LuceRPG.Game.Utility;
using LuceRPG.Game.WorldObjects;
using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private WithId.Model<WorldModule.Payload> idWorld;
    private readonly string clientId = "client-id";
    private readonly string playerName = "test-player";
    private readonly string interactionText = "Hello {player}!";

    private CursorStore CursorStore => Registry.Stores.Cursor;

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
                WorldObjectModule.TypeModule.Model.Wall, PointModule.create(2, 4), DirectionModule.Model.South
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
        var spawnPoint = PointModule.create(0, 1);

        var wallInteractionMap = Tuple.Create(wallModel.id, wallInteraction.id);
        var interactionMap = new FSharpMap<string, string>(wallInteractionMap.ToSingletonEnumerable());

        var world = WorldModule.createWithInteractions(
            "test", worldBounds, spawnPoint, WorldBackgroundModule.GreenGrass, objects, interactionMap
        );
        idWorld = WithId.create(world);
        var tsWorld = WithTimestamp.create(0, idWorld);

        var payload = new LoadWorldPayload(clientId, playerModel.id, tsWorld, interactions);
        testCommsService.OnLoad(payload);
    }

    [UnityTest]
    public IEnumerator WorldLoadsCorrectly()
    {
        // Scene correctly loaded
        var worldOverlord = GameObject.FindObjectOfType<WorldOverlord>();
        Assert.That(worldOverlord == null, Is.False);

        // Store data is correct
        var worldStore = Registry.Stores.World;
        Assert.That(worldStore.IdWorld, Is.EqualTo(idWorld));
        Assert.That(worldStore.PlayerId, Is.EqualTo(playerModel.id));
        Assert.That(worldStore.ClientId, Is.EqualTo(clientId));

        // Player is loaded correctly
        var playerObject = UniversalController.GetById(playerModel.id);
        Assert.That(playerObject, Is.Not.Null);
        var location = playerObject.transform.position;
        var expectedLocation = playerModel.GetBtmLeft();
        Assert.That(location, Is.EqualTo(expectedLocation));

        //Wall loads correctly
        var wallObject = UniversalController.GetById(wallModel.id);
        Assert.That(wallObject, Is.Not.Null);
        location = wallObject.gameObject.transform.position;
        expectedLocation = wallModel.GetBtmLeft();
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
        var serverEvent = WorldEventModule.asResult(testCommsService.LastIntentionId, idWorld.id, 0, serverEventT);
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
                    idWorld.id,
                    0,
                    WorldEventModule.Type.NewMoved(playerModel.id, DirectionModule.Model.South)
                )
            )
        };
        var getSinceResult =
                GetSinceResultModule.Payload.NewEvents(ListModule.OfSeq(worldEvents));
        var tsGetSinceResult = WithTimestamp.create(testTimestampProvider.Now, getSinceResult);

        testCommsService.OnUpdate(tsGetSinceResult);
        yield return null;

        // player object should target to be 1 square south
        var playerObject = UniversalController.GetById(playerModel.id);
        var expectedTarget = playerObject.GetModel().btmLeft.ToVector3();

        Assert.That(playerObject.Target, Is.EqualTo(expectedTarget));

        // wall object target should be unchanged
        var wallObject = UniversalController.GetById(wallModel.id);
        var wallUc = wallObject.GetComponent<UniversalController>();
        expectedTarget = wallObject.transform.position;
        Assert.That(wallUc.Target, Is.EqualTo(expectedTarget));

        // after a frame the player moves towards the target
        var priorPosition = playerObject.transform.position;
        yield return null;
        yield return null;
        var afterPosition = playerObject.transform.position;
        var positionChange = afterPosition - priorPosition;

        Assert.That(positionChange.x, Is.EqualTo(0));
        Assert.That(positionChange.y, Is.LessThan(0));

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
                        idWorld.id,
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
                        idWorld.id,
                        0,
                        WorldEventModule.Type.NewObjectRemoved(wallModel.id)
                )
            )
        };
        var getSinceResult =
            GetSinceResultModule.Payload.NewEvents(ListModule.OfSeq(worldEvents));
        var tsGetSinceResult = WithTimestamp.create(testTimestampProvider.Now, getSinceResult);

        testCommsService.OnUpdate(tsGetSinceResult);

        // Need to wait two frames since objects are destroyed at the end of the frame
        yield return null;
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

    [UnityTest]
    public IEnumerator WorldChangeHandledCorrectly()
    {
        var updateTime = 90L;

        // Player is at 4,8 and must be in bounds
        var bounds = RectModule.create(0, 0, 6, 12).ToSingletonEnumerable();
        var newWall = WithId.create(WorldObjectModule.create(
                WorldObjectModule.TypeModule.Model.Wall, PointModule.create(0, 4), DirectionModule.Model.South
            ));

        var newWallText = "New wall";
        var wallInteraction = WithId.create(InteractionModule.One.NewChat(newWallText).ToSingletonEnumerable());
        var interactionStore = InteractionStore.OfInteractions(wallInteraction);

        var objects = new WithId.Model<WorldObjectModule.Payload>[]
        {
            playerModel,
            newWall
        };

        var newInteractionMap = new FSharpMap<string, string>(
            Tuple.Create(newWall.id, wallInteraction.id).ToSingletonEnumerable()
        );
        var newWorld = WithId.create(
            WorldModule.createWithInteractions(
                "world-2", bounds, PointModule.zero, WorldBackgroundModule.GreenGrass, objects, newInteractionMap
            )
        );

        var getSinceResult = WithTimestamp.create(
            updateTime,
            GetSinceResultModule.Payload.NewWorldChanged(
                    newWorld,
                    WithId.toList(interactionStore.Value)
            )
        );
        testCommsService.OnUpdate(getSinceResult);
        yield return null;
        yield return null;

        // Player and new wall objects are present
        // Old wall is not present
        var playerObject = UniversalController.GetById(playerModel.id);
        Assert.That(playerObject, Is.Not.Null);

        var wallObject = UniversalController.GetById(wallModel.id);
        Assert.That(wallObject, Is.Null);

        var newWallObject = UniversalController.GetById(newWall.id);
        Assert.That(newWallObject, Is.Not.Null);

        // Backgrounds are correct
        var bcs = GameObject.FindObjectsOfType<BackgroundController>();
        Assert.That(bcs.Length, Is.EqualTo(1));
        Assert.That(bcs.Single().Rect, Is.EqualTo(bounds.Single()));
    }

    [UnityTest]
    public IEnumerator CursorDisplayCorrect()
    {
        // Wall at 2,4
        // Spawn point at 0,1
        // Player at 4,8
        // world bounds are 0,0 10,10

        // Objects correctly loaded
        var cursorOverlord = GameObject.FindObjectOfType<CursorOverlord>();
        var cursorDisplay = GameObject.FindObjectOfType<CursorDisplayController>();
        Assert.That(cursorOverlord == null, Is.False);
        Assert.That(cursorDisplay == null, Is.False);

        // Cursor is over no object
        testInputProvider.MousePosition = new Vector3(5.5f, 6.1f);
        yield return null;

        // Store is correct
        Assert.That(CursorStore.Position, Is.EqualTo(PointModule.create(5, 6)));
        Assert.That(CursorStore.CursorOverObject, Is.EqualTo(null));

        // Cursor display is correct on next frame
        yield return null;
        Assert.That(cursorDisplay.Position, Is.EqualTo(CursorStore.Position));
        Assert.That(cursorDisplay.Size, Is.EqualTo(PointModule.p1x1));
        Assert.That(cursorDisplay.ColourController.Colour, Is.EqualTo(CursorDisplayController.NoObjectColour));

        // Cursor is over the wall object
        testInputProvider.MousePosition = new Vector3(2.3f, 5.6f);
        yield return null;

        // store is correct
        Assert.That(CursorStore.Position, Is.EqualTo(PointModule.create(2, 5)));
        Assert.That(CursorStore.CursorOverObject, Is.EqualTo(wallModel));

        // Cursor display is correct on next frame
        yield return null;
        Assert.That(cursorDisplay.Position, Is.EqualTo(wallModel.value.btmLeft));
        Assert.That(cursorDisplay.Size, Is.EqualTo(WorldObjectModule.size(wallModel.value)));
        Assert.That(cursorDisplay.ColourController.Colour, Is.EqualTo(CursorDisplayController.OverObjectColor));

        // Cursor is over spawn point
        testInputProvider.MousePosition = new Vector3(0, 1.3f);
        yield return null;

        // Store is correct
        Assert.That(CursorStore.Position, Is.EqualTo(PointModule.create(0, 1)));
        Assert.That(CursorStore.CursorOverObject, Is.EqualTo(null));

        // Cursor display is correct on next frame
        yield return null;
        Assert.That(cursorDisplay.Position, Is.EqualTo(CursorStore.Position));
        Assert.That(cursorDisplay.Size, Is.EqualTo(PointModule.p1x1));
        Assert.That(cursorDisplay.ColourController.Colour, Is.EqualTo(CursorDisplayController.NoObjectColour));
    }

    [UnityTest]
    public IEnumerator PlayerLooksAtCursor()
    {
        // Player at 4,8

        // Objects correctly loaded
        var cursorOverlord = GameObject.FindObjectOfType<CursorOverlord>();
        var cursorDisplay = GameObject.FindObjectOfType<CursorDisplayController>();
        Assert.That(cursorOverlord == null, Is.False);
        Assert.That(cursorDisplay == null, Is.False);

        var playerObject = UniversalController.GetById(playerModel.id);
        Assert.That(playerObject == null, Is.False);

        // Cursor to east of player
        testInputProvider.MousePosition = new Vector3(6.1f, 8.3f);
        yield return null;
        Assert.That(playerObject.GetModel().facing, Is.EqualTo(DirectionModule.Model.East));
        yield return null;

        // Cursor to north of player
        testInputProvider.MousePosition = new Vector3(5.1f, 13.3f);
        yield return null;
        Assert.That(playerObject.GetModel().facing, Is.EqualTo(DirectionModule.Model.North));
        yield return null;

        // Cursor on player does not change direction
        testInputProvider.MousePosition = new Vector3(4.1f, 8.3f);
        yield return null;
        Assert.That(playerObject.GetModel().facing, Is.EqualTo(DirectionModule.Model.North));
        yield return null;

        // Cursor west of player
        testInputProvider.MousePosition = new Vector3(1.1f, 9.3f);
        yield return null;
        Assert.That(playerObject.GetModel().facing, Is.EqualTo(DirectionModule.Model.West));
        yield return null;

        // Cursor south of player
        testInputProvider.MousePosition = new Vector3(3f, -11.3f);
        yield return null;
        Assert.That(playerObject.GetModel().facing, Is.EqualTo(DirectionModule.Model.South));
        yield return null;
    }
}
