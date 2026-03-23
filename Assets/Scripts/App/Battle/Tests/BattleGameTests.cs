using System.Collections.Generic;
using System.Reflection;
using Madbox.Battle;
using Madbox.Enemies;
using Madbox.Levels;
using Madbox.Levels.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Battle.Tests
{
    public class BattleGameTests
    {
        [Test]
        public void Tick_AfterElapsedRule_CompletesWithReason()
        {
            TimeElapsedCompleteRule rule = ScriptableObject.CreateInstance<TimeElapsedCompleteRule>();
            SetPrivateField(rule, "elapsedSeconds", 1f);
            SetPrivateField(rule, "endReason", GameEndReason.Win);

            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            SetPrivateField(level, "sceneAssetReference", CreateSceneReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
            SetPrivateField(level, "enemyEntries", new List<LevelEnemySpawnEntry>());
            SetPrivateField(level, "gameRules", new List<LevelRuleDefinition> { rule });

            RuleHandlerRegistry ruleRegistry = new RuleHandlerRegistry();
            ruleRegistry.Register<TimeElapsedCompleteRule, TimeElapsedCompleteRuleHandler>();

            EnemyService enemyService = new EnemyService(new EnemyFactory());
            BattleGameFactory gameFactory = new BattleGameFactory();
            BattleGame game = gameFactory.CreateGame(level, enemyService, ruleRegistry);

            GameEndReason? completedReason = null;
            game.OnCompleted += reason => completedReason = reason;

            game.Start();
            game.Tick(1.1f);

            Assert.AreEqual(BattleGameState.Done, game.CurrentState);
            Assert.AreEqual(GameEndReason.Win, completedReason);
            Object.DestroyImmediate(level);
            Object.DestroyImmediate(rule);
        }

        private static AssetReference CreateSceneReference(string guid)
        {
            return new AssetReference(guid);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
