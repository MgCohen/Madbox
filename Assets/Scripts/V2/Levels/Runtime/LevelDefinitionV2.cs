using System;
using System.Collections.Generic;
using Madbox.V2.Enemies;
using UnityEngine;

namespace Madbox.V2.Levels
{
    [CreateAssetMenu(menuName = "Madbox/V2/Level Definition")]
    public sealed class LevelDefinitionV2 : ScriptableObject
    {
        public string LevelId => levelId;
        public string SceneKey => sceneKey;
        public IReadOnlyList<LevelEnemySpawnEntryV2> EnemyEntries => enemyEntries;

        [SerializeField] private string levelId = "level-v2-1";
        [SerializeField] private string sceneKey = "GameView";
        [SerializeField] private List<LevelEnemySpawnEntryV2> enemyEntries = new List<LevelEnemySpawnEntryV2>();

        public LevelRuntimeRequestV2 ToRuntimeRequest()
        {
            if (TryValidate(out string error) == false)
            {
                throw new InvalidOperationException(error);
            }

            List<LevelEnemySpawnPlanV2> plans = new List<LevelEnemySpawnPlanV2>(enemyEntries.Count);
            for (int i = 0; i < enemyEntries.Count; i++)
            {
                LevelEnemySpawnEntryV2 entry = enemyEntries[i];
                LevelEnemySpawnPlanV2 plan = new LevelEnemySpawnPlanV2(entry.EnemyPrefab, entry.Count);
                plans.Add(plan);
            }

            return new LevelRuntimeRequestV2(levelId, sceneKey, plans);
        }

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(levelId)) return SetError("Level id is required.", out error);
            if (string.IsNullOrWhiteSpace(sceneKey)) return SetError("Scene key is required.", out error);
            if (enemyEntries == null || enemyEntries.Count == 0) return SetError("At least one enemy entry is required.", out error);
            for (int i = 0; i < enemyEntries.Count; i++)
            {
                LevelEnemySpawnEntryV2 entry = enemyEntries[i];
                if (entry == null) return SetError($"Enemy entry at index {i} is null.", out error);
                if (entry.EnemyPrefab == null) return SetError($"Enemy entry at index {i} is missing prefab reference.", out error);
                if (entry.Count <= 0) return SetError($"Enemy entry at index {i} must have count greater than zero.", out error);
            }

            error = string.Empty;
            return true;
        }

        private bool SetError(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
