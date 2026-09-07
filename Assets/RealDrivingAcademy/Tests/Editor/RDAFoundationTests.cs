using NUnit.Framework;
using RDA.V50.Academy;
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
        }

        [Test]
        public void Evaluation_DefaultsAreSafe()
        {
            var state = new RDAEvaluationState();
            Assert.AreEqual(100, state.score);
            Assert.AreEqual(0, state.faults);
            Assert.IsFalse(state.failed);
        }
    }
}
