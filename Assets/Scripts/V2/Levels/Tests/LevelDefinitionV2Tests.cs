using System.Collections.Generic;
using System.Reflection;
using Madbox.V2.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.V2.Levels.Tests
{
    public class LevelDefinitionV2Tests
    {
        [Test]
        public void ToRuntimeRequest_WithValidData_ReturnsMappedEnemyPlans()
        {
            EnemyActor enemyPrefab = CreateEnemyPrefab();
            LevelEnemySpawnEntryV2 entry = CreateEnemyEntry(enemyPrefab, 3);
            LevelDefinitionV2 level = CreateLevel("level-v2-test", "GameView", entry);

            LevelRuntimeRequestV2 request = level.ToRuntimeRequest();

            Assert.AreEqual("level-v2-test", request.LevelId);
            Assert.AreEqual("GameView", request.SceneKey);
            Assert.AreEqual(1, request.Enemies.Count);
            Assert.AreSame(enemyPrefab, request.Enemies[0].EnemyPrefab);
            Assert.AreEqual(3, request.Enemies[0].Count);
            Object.DestroyImmediate(level);
            Object.DestroyImmediate(enemyPrefab.gameObject);
        }

        [Test]
        public void TryValidate_WithoutEnemyEntries_ReturnsFalse()
        {
            LevelDefinitionV2 level = CreateLevel("level-v2-empty", "GameView");

            bool isValid = level.TryValidate(out string error);

            Assert.IsFalse(isValid);
            Assert.AreEqual("At least one enemy entry is required.", error);
            Object.DestroyImmediate(level);
        }

        private static LevelDefinitionV2 CreateLevel(string id, string sceneKey, params LevelEnemySpawnEntryV2[] entries)
        {
            LevelDefinitionV2 level = ScriptableObject.CreateInstance<LevelDefinitionV2>();
            SetPrivateField(level, "levelId", id);
            SetPrivateField(level, "sceneKey", sceneKey);
            SetPrivateField(level, "enemyEntries", new List<LevelEnemySpawnEntryV2>(entries));
            return level;
        }

        private static LevelEnemySpawnEntryV2 CreateEnemyEntry(EnemyActor prefab, int count)
        {
            LevelEnemySpawnEntryV2 entry = new LevelEnemySpawnEntryV2();
            SetPrivateField(entry, "enemyPrefab", prefab);
            SetPrivateField(entry, "count", count);
            return entry;
        }

        private static EnemyActor CreateEnemyPrefab()
        {
            GameObject go = new GameObject("EnemyPrefab");
            EnemyActor actor = go.AddComponent<EnemyActor>();
            return actor;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
