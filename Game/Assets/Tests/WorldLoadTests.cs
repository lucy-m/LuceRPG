using LuceRPG.Game;
using LuceRPG.Game.Providers;
using LuceRPG.Game.Services;
using LuceRPG.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Assets.Resources.Tests
{
    public class WorldLoadTests
    {
        private TestCommsService testCommsService;
        private TestInputProvider testInputProvider;
        private TestTimestampProvider testTimestampProvider;

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

            SceneManager.LoadScene("GameLoader", LoadSceneMode.Additive);

            yield return null;
        }
    }
}
