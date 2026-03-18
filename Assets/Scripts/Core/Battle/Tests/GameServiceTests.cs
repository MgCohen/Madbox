using System.Threading;
using System.Threading.Tasks;
using Madbox.Battle.Services;
using Madbox.Levels;
using Madbox.Levels.Contracts;
using Madbox.Levels.Behaviors;
using Madbox.Levels.Rules;
using NUnit.Framework;
#pragma warning disable SCA0003
#pragma warning disable SCA0005
#pragma warning disable SCA0006

namespace Madbox.Battle.Tests
{
    public sealed class GameServiceTests
    {
        [Test]
        public void StartAsync_CreatesRunningGameFromLevelService()
        {
            ILevelService levels = new FakeLevelService();
            GameService service = new GameService(levels);
            LevelId selectedLevel = new LevelId("selected-level");

            Game game = service.StartAsync(selectedLevel, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsNotNull(game);
            Assert.AreEqual(GameState.Running, game.CurrentState);
            FakeLevelService fake = levels as FakeLevelService;
            Assert.IsNotNull(fake);
            Assert.AreEqual("selected-level", fake.LastRequestedLevelId.Value);
        }

        private sealed class FakeLevelService : ILevelService
        {
            public LevelId DefaultLevelId => new LevelId("whitebox-level-1");
            public LevelId LastRequestedLevelId { get; private set; }

            public Task<LevelDefinition> LoadAsync(LevelId levelId, CancellationToken cancellationToken = default)
            {
                LastRequestedLevelId = levelId;
                LevelDefinition level = new LevelDefinition(
                    levelId,
                    1,
                    new[]
                    {
                        new LevelEnemyDefinition(
                            new EnemyDefinition(
                                new EntityId("enemy"),
                                3,
                                new EnemyBehaviorDefinition[] { new MovementBehaviorDefinition(0.1f, 1f) }),
                            1)
                    },
                    new LevelGameRuleDefinition[] { new TimeLimitLoseRuleDefinition(3f) });
                return Task.FromResult(level);
            }
        }
    }
}
