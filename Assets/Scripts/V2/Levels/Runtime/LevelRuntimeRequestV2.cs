using System;
using System.Collections.Generic;

namespace Madbox.V2.Levels
{
    public sealed class LevelRuntimeRequestV2
    {
        public LevelRuntimeRequestV2(string levelId, string sceneKey, IReadOnlyList<LevelEnemySpawnPlanV2> enemies)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                throw new ArgumentException("Level id is required.", nameof(levelId));
            }

            if (string.IsNullOrWhiteSpace(sceneKey))
            {
                throw new ArgumentException("Scene key is required.", nameof(sceneKey));
            }

            if (enemies == null || enemies.Count == 0)
            {
                throw new ArgumentException("At least one enemy spawn is required.", nameof(enemies));
            }

            LevelId = levelId;
            SceneKey = sceneKey;
            Enemies = enemies;
        }

        public string LevelId { get; }
        public string SceneKey { get; }
        public IReadOnlyList<LevelEnemySpawnPlanV2> Enemies { get; }
    }
}
