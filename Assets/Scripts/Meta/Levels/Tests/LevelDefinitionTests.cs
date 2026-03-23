using System.Collections.Generic;
using System.Reflection;
using Madbox.Enemies;
using Madbox.Levels.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Levels.Tests
{
    public class LevelDefinitionTests
    {
        [Test]
        public void EnemyEntries_AreExposedFromSerializedLevel()
        {
            AssetReference sceneAssetReference = CreateSceneReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            AssetReferenceT<Enemy> enemyAssetReference = CreateEnemyReference("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
            LevelEnemySpawnEntry entry = CreateEnemyEntry(enemyAssetReference, 3);
            LevelDefinition level = CreateLevel(7, sceneAssetReference, entry);

            Assert.AreEqual(7, level.LevelId);
            Assert.AreSame(sceneAssetReference, level.SceneAssetReference);
            Assert.AreEqual(1, level.EnemyEntries.Count);
            Assert.AreSame(enemyAssetReference, level.EnemyEntries[0].EnemyAssetReference);
            Assert.AreEqual(3, level.EnemyEntries[0].Count);
            Object.DestroyImmediate(level);
        }

        private static LevelDefinition CreateLevel(int levelId, AssetReference sceneAssetReference, params LevelEnemySpawnEntry[] entries)
        {
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            SetPrivateField(level, "levelId", levelId);
            SetPrivateField(level, "sceneAssetReference", sceneAssetReference);
            SetPrivateField(level, "enemyEntries", new List<LevelEnemySpawnEntry>(entries));
            SetPrivateField(level, "gameRules", new List<LevelRuleDefinition>());
            return level;
        }

        private static LevelEnemySpawnEntry CreateEnemyEntry(AssetReferenceT<Enemy> enemyAssetReference, int count)
        {
            LevelEnemySpawnEntry entry = new LevelEnemySpawnEntry();
            SetPrivateField(entry, "enemyAssetReference", enemyAssetReference);
            SetPrivateField(entry, "count", count);
            return entry;
        }

        private static AssetReference CreateSceneReference(string guid)
        {
            return new AssetReference(guid);
        }

        private static AssetReferenceT<Enemy> CreateEnemyReference(string guid)
        {
            return new AssetReferenceT<Enemy>(guid);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
