using System.Collections.Generic;
using System.Reflection;
using Madbox.V2.Battle;
using Madbox.V2.Enemies;
using Madbox.V2.Levels;
using Madbox.V2.Levels.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.V2.Battle.Tests
{
    public class GameV2Tests
    {
        [Test]
        public void Tick_AfterElapsedRule_CompletesWithReason()
        {
            TimeElapsedCompleteRuleV2 rule = ScriptableObject.CreateInstance<TimeElapsedCompleteRuleV2>();
            SetPrivateField(rule, "elapsedSeconds", 1f);
            SetPrivateField(rule, "endReason", GameEndReasonV2.Win);

            LevelDefinitionV2 level = ScriptableObject.CreateInstance<LevelDefinitionV2>();
            SetPrivateField(level, "sceneAssetReference", CreateSceneReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
            SetPrivateField(level, "enemyEntries", new List<LevelEnemySpawnEntryV2>());
            SetPrivateField(level, "gameRules", new List<LevelRuleDefinitionV2> { rule });

            RuleHandlerRegistryV2 ruleRegistry = new RuleHandlerRegistryV2();
            ruleRegistry.Register<TimeElapsedCompleteRuleV2, TimeElapsedCompleteRuleHandlerV2>();

            EnemyServiceV2 enemyService = new EnemyServiceV2(new EnemyFactoryV2());
            GameFactoryV2 gameFactory = new GameFactoryV2();
            GameV2 game = gameFactory.CreateGame(level, enemyService, ruleRegistry);

            GameEndReasonV2? completedReason = null;
            game.OnCompleted += reason => completedReason = reason;

            game.Start();
            game.Tick(1.1f);

            Assert.AreEqual(GameStateV2.Done, game.CurrentState);
            Assert.AreEqual(GameEndReasonV2.Win, completedReason);
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
