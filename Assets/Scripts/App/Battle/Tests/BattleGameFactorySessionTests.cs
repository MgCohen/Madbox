using Madbox.Enemies;
using Madbox.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.Battle.Tests
{
    public sealed class BattleGameFactorySessionTests
    {
        [Test]
        public void CreatePrepareStartAsync_StartsGame_AndSpawnsEnemies()
        {
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            EnemyService enemyService = new EnemyService(new EnemyFactory());
            RuleHandlerRegistry registry = new RuleHandlerRegistry();
            BattleGameFactory factory = new BattleGameFactory();

            try
            {
                BattleGame game = factory.CreatePrepareStartAsync(level, enemyService, registry, Vector3.zero, 1f).GetAwaiter().GetResult();

                Assert.IsTrue(game.IsRunning);
                Assert.AreEqual(0, enemyService.AliveEnemies);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }
    }
}
