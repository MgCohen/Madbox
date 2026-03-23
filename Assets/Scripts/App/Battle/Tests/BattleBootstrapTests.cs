using System;
using Madbox.Enemies;
using Madbox.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.Battle.Tests
{
    public sealed class BattleBootstrapTests
    {
        [Test]
        public void StartBattleAsync_Throws_WhenLevelNull()
        {
            BattleBootstrap bootstrap = new BattleBootstrap();
            EnemyService enemyService = new EnemyService(new EnemyFactory());
            RuleHandlerRegistry registry = new RuleHandlerRegistry();

            Assert.Throws<ArgumentNullException>(() =>
                bootstrap.StartBattleAsync(null, enemyService, registry, Vector3.zero, 1f).GetAwaiter().GetResult());
        }

        [Test]
        public void StartBattleAsync_Throws_WhenEnemyServiceNull()
        {
            BattleBootstrap bootstrap = new BattleBootstrap();
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            RuleHandlerRegistry registry = new RuleHandlerRegistry();

            try
            {
                Assert.Throws<ArgumentNullException>(() =>
                    bootstrap.StartBattleAsync(level, null, registry, Vector3.zero, 1f).GetAwaiter().GetResult());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void StartBattleAsync_Throws_WhenRuleRegistryNull()
        {
            BattleBootstrap bootstrap = new BattleBootstrap();
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            EnemyService enemyService = new EnemyService(new EnemyFactory());

            try
            {
                Assert.Throws<ArgumentNullException>(() =>
                    bootstrap.StartBattleAsync(level, enemyService, null, Vector3.zero, 1f).GetAwaiter().GetResult());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(level);
            }
        }
    }
}
