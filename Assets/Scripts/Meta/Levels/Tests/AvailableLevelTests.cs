using GameModuleDTO.Modules.Level;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.Levels.Tests
{
    public sealed class AvailableLevelTests
    {
        [Test]
        public void IsBlocked_IsTrue_WhenAvailabilityStateIsBlocked()
        {
            LevelDefinition def = ScriptableObject.CreateInstance<LevelDefinition>();
            try
            {
                var sut = new AvailableLevel(def, LevelAvailabilityState.Blocked);
                Assert.That(sut.IsBlocked, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(def);
            }
        }

        [TestCase(LevelAvailabilityState.Unlocked)]
        [TestCase(LevelAvailabilityState.Complete)]
        public void IsBlocked_IsFalse_WhenNotBlocked(LevelAvailabilityState state)
        {
            LevelDefinition def = ScriptableObject.CreateInstance<LevelDefinition>();
            try
            {
                var sut = new AvailableLevel(def, state);
                Assert.That(sut.IsBlocked, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(def);
            }
        }
    }
}
