using Madbox.Enemies.Behaviors;
using Madbox.Enemies.Services;
using Madbox.Levels;
using Madbox.Levels.Behaviors;
using NUnit.Framework;

namespace Madbox.Enemies.Tests
{
    public class EnemyServiceTests
    {
        private const int contactDamage = 5;
        private const float contactRange = 1f;
        private const float contactCooldown = 2f;

        [Test]
        public void Constructor_WhenLevelHasEnemies_SetsAliveEnemiesCount()
        {
            LevelDefinition level = CreateLevel(enemyCount: 2);

            EnemyService service = new EnemyService(level);

            Assert.AreEqual(2, service.AliveEnemies);
            Assert.AreEqual(0, service.DeadEnemies);
        }

        [Test]
        public void ContactAttackBehaviorRuntime_WhenConsumed_StartsCooldown()
        {
            ContactAttackBehaviorRuntime runtime = CreateContactAttackRuntime();
            ConsumeSequenceResult sequence = BuildExecuteConsumeSequence(runtime);
            BuildAssertConsumeSequence(sequence);
        }

        private static LevelDefinition CreateLevel(int enemyCount)
        {
            EnemyDefinition enemy = CreateEnemyDefinition();
            LevelEnemyDefinition levelEnemy = new LevelEnemyDefinition(enemy, enemyCount);
            LevelId levelId = new LevelId("level-enemy-tests");
            return new LevelDefinition(levelId, 0, new[] { levelEnemy });
        }

        private static EnemyDefinition CreateEnemyDefinition()
        {
            EntityId enemyTypeId = new EntityId("enemy-type");
            EnemyBehaviorDefinition[] behaviors = CreateEnemyBehaviors();
            return new EnemyDefinition(enemyTypeId, 20, behaviors);
        }

        private static EnemyBehaviorDefinition[] CreateEnemyBehaviors()
        {
            MovementBehaviorDefinition movement = new MovementBehaviorDefinition(1f, 3f);
            ContactAttackBehaviorDefinition contactAttack = new ContactAttackBehaviorDefinition(contactDamage, 0.5f, contactCooldown);
            return new EnemyBehaviorDefinition[] { movement, contactAttack };
        }

        private static ContactAttackBehaviorRuntime CreateContactAttackRuntime()
        {
            ContactAttackBehaviorDefinition definition = new ContactAttackBehaviorDefinition(contactDamage, contactRange, contactCooldown);
            return new ContactAttackBehaviorRuntime(definition);
        }

        private static ConsumeSequenceResult BuildExecuteConsumeSequence(ContactAttackBehaviorRuntime runtime)
        {
            ConsumeSequenceResult result = new ConsumeSequenceResult();
            BuildConsumeThreeTimes(runtime, result);
            return result;
        }

        private static void BuildConsumeThreeTimes(ContactAttackBehaviorRuntime runtime, ConsumeSequenceResult result)
        {
            result.FirstConsume = runtime.TryConsume(contactDamage, out int firstDamage);
            result.SecondConsume = runtime.TryConsume(contactDamage, out int secondDamage);
            runtime.Tick(contactCooldown);
            result.ThirdConsume = runtime.TryConsume(contactDamage, out int thirdDamage);
            result.FirstDamage = firstDamage;
            result.SecondDamage = secondDamage;
            result.ThirdDamage = thirdDamage;
        }

        private static void BuildAssertConsumeSequence(ConsumeSequenceResult sequence)
        {
            Assert.IsTrue(sequence.FirstConsume);
            Assert.AreEqual(contactDamage, sequence.FirstDamage);
            Assert.IsFalse(sequence.SecondConsume);
            Assert.AreEqual(0, sequence.SecondDamage);
            Assert.IsTrue(sequence.ThirdConsume);
            Assert.AreEqual(contactDamage, sequence.ThirdDamage);
        }

        private sealed class ConsumeSequenceResult
        {
            public bool FirstConsume { get; set; }
            public bool SecondConsume { get; set; }
            public bool ThirdConsume { get; set; }
            public int FirstDamage { get; set; }
            public int SecondDamage { get; set; }
            public int ThirdDamage { get; set; }
        }
    }
}


