using LuceRPG.Models;
using NUnit.Framework;
using System.Linq;
using LuceRPG.Server;
using LuceRPG.Server.Processors;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuceRPG.Server.Test
{
    public class Tests
    {
        private WorldModule.Model initialWorld;
        private WorldEventsStorer storer;
        private IntentionQueue queue;
        private IntentionProcessor processor;

        [SetUp]
        public void Setup()
        {
            var worldBounds = new RectModule.Model[]
            {
                new RectModule.Model(PointModule.create(0, 10), PointModule.create(10, 10))
            };
            var spawnPoint = PointModule.create(10, 10);

            initialWorld = WorldModule.empty(worldBounds, spawnPoint);
            storer = new WorldEventsStorer(initialWorld);
            queue = new IntentionQueue();
            processor = new IntentionProcessor(storer, queue, new NullLogger<IntentionProcessor>());
        }

        [Test]
        public void EmptyIntentionQueue()
        {
            var now = 100L;
            processor.ProcessAt(now);

            // world is unchanged
            Assert.That(storer.CurrentWorld, Is.EqualTo(initialWorld));
        }
    }
}
