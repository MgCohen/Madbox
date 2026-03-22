using System.Collections.Generic;
using System.Reflection;
using GameModuleDTO.Modules.Level;
using NUnit.Framework;

namespace Madbox.Level.Tests
{
    public sealed class LevelGameDataTests
    {
        [Test]
        public void Constructor_Completed1InOrder1234_StatesMatchLinearProgression()
        {
            LevelPersistence persistence = new LevelPersistence();
            persistence.AddCompletedLevel(1);
            LevelConfig config = CreateConfig(1, 2, 3, 4);

            LevelGameData data = new LevelGameData(persistence, config);

            Assert.That(data.States.Count, Is.EqualTo(4));
            Assert.That(data.States[0].LevelId, Is.EqualTo(1));
            Assert.That(data.States[0].State, Is.EqualTo(LevelAvailabilityState.Complete));
            Assert.That(data.States[1].State, Is.EqualTo(LevelAvailabilityState.Unlocked));
            Assert.That(data.States[2].State, Is.EqualTo(LevelAvailabilityState.Blocked));
            Assert.That(data.States[3].State, Is.EqualTo(LevelAvailabilityState.Blocked));
        }

        [Test]
        public void Constructor_NoneCompleted_FirstUnlocked()
        {
            LevelPersistence persistence = new LevelPersistence();
            LevelConfig config = CreateConfig(1, 2, 3, 4);

            LevelGameData data = new LevelGameData(persistence, config);

            Assert.That(data.States[0].State, Is.EqualTo(LevelAvailabilityState.Unlocked));
            Assert.That(data.States[1].State, Is.EqualTo(LevelAvailabilityState.Blocked));
        }

        private static LevelConfig CreateConfig(params int[] ids)
        {
            LevelConfig config = new LevelConfig();
            FieldInfo field = typeof(LevelConfig).GetField("_levels", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(config, new List<int>(ids));
            return config;
        }
    }
}
