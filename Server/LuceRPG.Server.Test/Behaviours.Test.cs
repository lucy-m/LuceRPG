using LuceRPG.Models;
using LuceRPG.Server.Core;
using LuceRPG.Server.Processors;
using LuceRPG.Server.Storer;
using LuceRPG.Utility;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FSharp.Collections;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuceRPG.Server.Test
{
    public class BehavioursTests
    {
        protected WithId.Model<WorldModule.Payload> initialWorld;

        protected WorldEventsStorer worldStorer;
        protected IntentionQueue intentionQueue;
        protected BehaviourMapStorer behaviourMapStorer;

        protected IntentionProcessor intentionProcessor;
        protected BehaviourProcessor behaviourProcessor;
        protected TestTimestampProvider timestampProvider;

        protected string npcId = "npc";
        protected PointModule.Model npcStart;

        protected WithId.Model<WorldObjectModule.Payload> GetNpc()
        {
            var world = worldStorer.GetWorld(initialWorld.id);
            return MapModule.Find(npcId, world.value.objects);
        }

        [SetUp]
        public void Setup()
        {
            timestampProvider = new TestTimestampProvider() { Now = 100L };
            var logService = new TestCsvLogService();

            var worldBounds = RectModule.create(0, 0, 10, 10).ToSingletonEnumerable();
            var spawnPoint = PointModule.create(0, 0);

            npcStart = PointModule.create(4, 4);

            var npc = WithId.useId(
                npcId,
                WorldObjectModule.create(
                    WorldObjectModule.TypeModule.Model.NewNPC(
                        CharacterDataModule.randomized("NPC")
                    ),
                    npcStart,
                    DirectionModule.Model.South
                )
            );

            initialWorld = WithId.create(
                WorldModule.createWithObjs(
                    "TestWorld",
                    worldBounds,
                    spawnPoint,
                    WorldBackgroundModule.GreenGrass,
                    npc.ToSingletonEnumerable()
                )
            );

            var worldCollection = WorldCollectionModule.createWithoutInteractions(
                initialWorld.id,
                initialWorld.ToSingletonEnumerable()
            );

            behaviourMapStorer = new BehaviourMapStorer(worldCollection);
            worldStorer = new WorldEventsStorer(worldCollection, timestampProvider);
            intentionQueue = new IntentionQueue(timestampProvider);
            intentionProcessor = new IntentionProcessor(
                new NullLogger<IntentionProcessor>(),
                worldStorer,
                intentionQueue,
                logService,
                timestampProvider
            );
            behaviourProcessor = new BehaviourProcessor(
                timestampProvider,
                worldStorer,
                behaviourMapStorer,
                intentionQueue
            );
        }

        [Test]
        public void NoBehaviour_DoesNothing()
        {
            behaviourProcessor.Process();

            Assert.That(intentionQueue.Queue.Count, Is.EqualTo(0));
        }

        protected class PatrolBehaviour : BehavioursTests
        {
            protected readonly long waitTime = 200L;

            [SetUp]
            public void SetUp_PatrolBehaviour()
            {
                var moveSouth = Tuple.Create(DirectionModule.Model.South, (byte)1);
                var patrolBehaviour = BehaviourModule.patrolUniform(
                    moveSouth.ToSingletonEnumerable(),
                    TimeSpan.FromTicks(waitTime),
                    true
                );

                var behaviourMap = MapModule.OfSeq(
                    Tuple.Create(npcId, patrolBehaviour).ToSingletonEnumerable()
                );

                behaviourMapStorer.Maps[initialWorld.id] = behaviourMap;
            }

            [Test]
            public void ProcessProducesCorrectIntentions()
            {
                behaviourProcessor.Process();

                // intention queue should have a move south intention
                var intentions = intentionQueue.Queue.ToArray();
                Assert.That(intentions.Length, Is.EqualTo(1));
                var intention =
                    intentions.First().Intention.tsIntention.value.value.t
                    as IntentionModule.Type.Move;

                Assert.That(intention, Is.Not.Null);
                Assert.That(intention.Item1, Is.EqualTo(npcId));
                Assert.That(intention.Item2, Is.EqualTo(DirectionModule.Model.South));
                Assert.That(intention.Item3, Is.EqualTo(1));
            }
        }
    }
}
