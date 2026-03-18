using System;
using NUnit.Framework;
#pragma warning disable SCA0003
#pragma warning disable SCA0005
#pragma warning disable SCA0006
#pragma warning disable SCA0023

namespace Madbox.Levels.Tests
{
    public class LevelDefinitionsTests
    {
        [Test]
        public void EnemyDefinition_WhenInputsAreValid_CreatesInstance()
        {
            EnemyDefinition definition = CreateEnemyDefinition("enemy-a");

            Assert.AreEqual("enemy-a", definition.EnemyTypeId.Value);
            Assert.AreEqual(20, definition.MaxHealth);
            Assert.AreEqual(2, definition.Behaviors.Count);
        }

        [Test]
        public void EnemyDefinition_WhenEnemyTypeIdIsMissing_ThrowsArgumentException()
        {
            TestDelegate action = () => new EnemyDefinition(new EntityId(""), 20, CreateBehaviorSet());

            Assert.Throws<ArgumentException>(action);
        }

        [Test]
        public void EnemyDefinition_WhenBehaviorContainsNull_ThrowsArgumentException()
        {
            EnemyBehaviorDefinition[] behaviors =
            {
                new MovementBehaviorDefinition(1f, 3f),
                null
            };
            TestDelegate action = () => new EnemyDefinition(new EntityId("enemy-a"), 20, behaviors);

            Assert.Throws<ArgumentException>(action);
        }

        [Test]
        public void LevelEnemyDefinition_WhenCountIsZero_ThrowsArgumentOutOfRangeException()
        {
            EnemyDefinition enemy = CreateEnemyDefinition("enemy-a");
            TestDelegate action = () => new LevelEnemyDefinition(enemy, 0);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Test]
        public void LevelDefinition_WhenInputsAreValid_CreatesInstance()
        {
            LevelEnemyDefinition[] enemies =
            {
                new LevelEnemyDefinition(CreateEnemyDefinition("enemy-a"), 1),
                new LevelEnemyDefinition(CreateEnemyDefinition("enemy-b"), 2)
            };
            LevelDefinition level = new LevelDefinition(new LevelId("level-1"), 10, enemies);

            Assert.AreEqual("level-1", level.LevelId.Value);
            Assert.AreEqual(10, level.GoldReward);
            Assert.AreEqual(2, level.Enemies.Count);
        }

        [Test]
        public void LevelDefinition_WhenGoldRewardIsNegative_ThrowsArgumentOutOfRangeException()
        {
            LevelEnemyDefinition[] enemies = { new LevelEnemyDefinition(CreateEnemyDefinition("enemy-a"), 1) };
            TestDelegate action = () => new LevelDefinition(new LevelId("level-1"), -1, enemies);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Test]
        public void LevelDefinition_WhenEnemyListIsEmpty_ThrowsArgumentException()
        {
            LevelEnemyDefinition[] enemies = Array.Empty<LevelEnemyDefinition>();
            TestDelegate action = () => new LevelDefinition(new LevelId("level-1"), 10, enemies);

            Assert.Throws<ArgumentException>(action);
        }

        [Test]
        public void LevelDefinition_WhenEnemyTypesAreDuplicated_ThrowsArgumentException()
        {
            LevelEnemyDefinition[] enemies =
            {
                new LevelEnemyDefinition(CreateEnemyDefinition("enemy-a"), 1),
                new LevelEnemyDefinition(CreateEnemyDefinition("enemy-a"), 2)
            };
            TestDelegate action = () => new LevelDefinition(new LevelId("level-1"), 10, enemies);

            Assert.Throws<ArgumentException>(action);
        }

        private EnemyDefinition CreateEnemyDefinition(string enemyType)
        {
            return new EnemyDefinition(new EntityId(enemyType), 20, CreateBehaviorSet());
        }

        private EnemyBehaviorDefinition[] CreateBehaviorSet()
        {
            EnemyBehaviorDefinition[] behaviors =
            {
                new MovementBehaviorDefinition(1.5f, 4f),
                new ContactAttackBehaviorDefinition(5, 0.8f, 1.1f)
            };

            return behaviors;
        }
    }
}
