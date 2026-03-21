using System.Collections.Generic;
using System.Reflection;
using Madbox.V2.Enemies;
using Madbox.V2.Levels.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.V2.Levels.Tests
{
    public class LevelDefinitionV2Tests
    {
        [Test]
        public void EnemyEntries_AreExposedFromSerializedLevel()
        {
            AssetReference sceneAssetReference = CreateSceneReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            AssetReferenceT<EnemyActor> enemyAssetReference = CreateEnemyReference("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
            LevelEnemySpawnEntryV2 entry = CreateEnemyEntry(enemyAssetReference, 3);
            LevelDefinitionV2 level = CreateLevel(sceneAssetReference, entry);

            Assert.AreSame(sceneAssetReference, level.SceneAssetReference);
            Assert.AreEqual(1, level.EnemyEntries.Count);
            Assert.AreSame(enemyAssetReference, level.EnemyEntries[0].EnemyAssetReference);
            Assert.AreEqual(3, level.EnemyEntries[0].Count);
            Object.DestroyImmediate(level);
        }

        private static LevelDefinitionV2 CreateLevel(AssetReference sceneAssetReference, params LevelEnemySpawnEntryV2[] entries)
        {
            LevelDefinitionV2 level = ScriptableObject.CreateInstance<LevelDefinitionV2>();
            SetPrivateField(level, "sceneAssetReference", sceneAssetReference);
            SetPrivateField(level, "enemyEntries", new List<LevelEnemySpawnEntryV2>(entries));
            SetPrivateField(level, "gameRules", new List<LevelRuleDefinitionV2>());
            return level;
        }

        private static LevelEnemySpawnEntryV2 CreateEnemyEntry(AssetReferenceT<EnemyActor> enemyAssetReference, int count)
        {
            LevelEnemySpawnEntryV2 entry = new LevelEnemySpawnEntryV2();
            SetPrivateField(entry, "enemyAssetReference", enemyAssetReference);
            SetPrivateField(entry, "count", count);
            return entry;
        }

        private static AssetReference CreateSceneReference(string guid)
        {
            return new AssetReference(guid);
        }

        private static AssetReferenceT<EnemyActor> CreateEnemyReference(string guid)
        {
            return new AssetReferenceT<EnemyActor>(guid);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
