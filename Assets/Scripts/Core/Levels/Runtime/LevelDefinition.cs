using System;
using System.Collections.Generic;
#pragma warning disable SCA0006

namespace Madbox.Levels
{
    public class LevelDefinition
    {
        public LevelDefinition(LevelId levelId, int goldReward, IReadOnlyList<LevelEnemyDefinition> enemies)
        {
            if (levelId == null)
            {
                throw new ArgumentNullException(nameof(levelId));
            }

            if (string.IsNullOrWhiteSpace(levelId.Value))
            {
                throw new ArgumentException("Level id is required.", nameof(levelId));
            }

            if (goldReward < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(goldReward), "Gold reward cannot be negative.");
            }

            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            if (enemies.Count == 0)
            {
                throw new ArgumentException("At least one enemy entry is required.", nameof(enemies));
            }

            EnsureEnemyEntries(enemies);

            LevelId = levelId;
            GoldReward = goldReward;
            Enemies = enemies;
        }

        public LevelId LevelId { get; }

        public int GoldReward { get; }

        public IReadOnlyList<LevelEnemyDefinition> Enemies { get; }

        private void EnsureEnemyEntries(IReadOnlyList<LevelEnemyDefinition> enemies)
        {
            HashSet<EntityId> seenEnemyTypes = new HashSet<EntityId>();
            for (int i = 0; i < enemies.Count; i++)
            {
                LevelEnemyDefinition entry = enemies[i];
                if (entry == null)
                {
                    throw new ArgumentException("Enemy entries cannot contain null.", nameof(enemies));
                }

                EntityId enemyTypeId = entry.Enemy.EnemyTypeId;
                if (seenEnemyTypes.Add(enemyTypeId) == false)
                {
                    throw new ArgumentException($"Duplicate enemy type id '{enemyTypeId.Value}'.", nameof(enemies));
                }
            }
        }
    }
}
