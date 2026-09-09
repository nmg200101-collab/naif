using NUnit.Framework;
using UnityEngine;
using RDA.V50.Academy;
using RDA.V50.Core;
using RDA.V50.Input;
using RDA.V50.Save;

namespace RDA.V50.Tests
{
    public sealed class RDAFoundationTests
    {
        [Test]
        public void NewProgress_HasExpectedDefaults()
        {
            var p = new RDAPlayerProgress();
            Assert.AreEqual(1, p.schemaVersion);
            Assert.IsTrue(p.guest);
            Assert.AreEqual(0, p.totalScore);
            Assert.AreEqual(RDASceneFlow.MainMenu, p.lastScene);
        }

        [Test]
        public void Evaluation_DefaultsAndFaultPenaltyAreSafe()
        {
            var go = new GameObject("EvaluatorTest");
            try
            {
                var evaluator = go.AddComponent<RDADrivingEvaluator>();
                Assert.AreEqual(100, evaluator.State.score);
                evaluator.RegisterFault(25);
                Assert.AreEqual(75, evaluator.State.score);
                Assert.AreEqual(1, evaluator.State.faults);
                Assert.IsFalse(evaluator.State.failed);
                evaluator.RegisterFault(30);
                Assert.AreEqual(45, evaluator.State.score);
                Assert.IsTrue(evaluator.State.failed);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void InputState_ClampsValuesAndResets()
        {
            var go = new GameObject("InputTest");
            try
            {
                var input = go.AddComponent<RDAInputState>();
                input.SetSteering(2f);
                input.SetThrottle(-1f);
                input.SetBrake(3f);
                input.SetClutch(0.5f);
                input.SetHandbrake(true);
                Assert.AreEqual(1f, input.Steering);
                Assert.AreEqual(0f, input.Throttle);
                Assert.AreEqual(1f, input.Brake);
                Assert.AreEqual(0.5f, input.Clutch);
                Assert.IsTrue(input.Handbrake);
                input.ResetAll();
                Assert.AreEqual(0f, input.Steering);
                Assert.AreEqual(0f, input.Throttle);
                Assert.AreEqual(0f, input.Brake);
                Assert.AreEqual(0f, input.Clutch);
                Assert.IsFalse(input.Handbrake);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SceneNames_AreStableFoundationContract()
        {
            Assert.AreEqual("RDA_Boot", RDASceneFlow.Boot);
            Assert.AreEqual("RDA_Login", RDASceneFlow.Login);
            Assert.AreEqual("RDA_MainMenu", RDASceneFlow.MainMenu);
        }
    }
}
