using System.Collections.Generic;
using System.Threading;
using Madbox.Levels.Behaviors;
using Madbox.Levels.Contracts;
using Madbox.Levels.Rules;
using NUnit.Framework;
#pragma warning disable SCA0003
#pragma warning disable SCA0005
#pragma warning disable SCA0006

namespace Madbox.Levels.Tests
{
    public sealed class LevelServiceTests
    {
        [Test]
        public void DefaultLevelId_UsesFirstPreloadedLevel()
        {
            LevelDefinition first = CreateLevel("level-1");
            LevelDefinition second = CreateLevel("level-2");
            ILevelService service = new LevelService(new List<LevelDefinition> { first, second });

            Assert.AreEqual("level-1", service.DefaultLevelId.Value);
        }

        [Test]
        public void LoadAsync_WhenLevelExists_ReturnsMatchingLevel()
        {
            LevelDefinition first = CreateLevel("level-1");
            LevelDefinition second = CreateLevel("level-2");
            ILevelService service = new LevelService(new List<LevelDefinition> { first, second });

            LevelDefinition loaded = service.LoadAsync(new LevelId("level-2"), CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreSame(second, loaded);
        }

        [Test]
        public void Constructor_WhenNoLevels_UsesFallbackLevel()
        {
            ILevelService service = new LevelService(new List<LevelDefinition>());
            Assert.AreEqual("whitebox-level-1", service.DefaultLevelId.Value);
        }

        private static LevelDefinition CreateLevel(string id)
        {
            EnemyBehaviorDefinition[] behavior = { new MovementBehaviorDefinition(0.1f, 1f) };
            EnemyDefinition enemy = new EnemyDefinition(new EntityId($"enemy-{id}"), 10, behavior);
            LevelEnemyDefinition[] enemies = { new LevelEnemyDefinition(enemy, 1) };
            LevelGameRuleDefinition[] rules = { new TimeLimitLoseRuleDefinition(3f) };
            return new LevelDefinition(new LevelId(id), 1, enemies, rules);
        }
    }
}
