using System;
using Madbox.V2.Enemies;

namespace Madbox.V2.Levels
{
    public readonly struct LevelEnemySpawnPlanV2
    {
        public LevelEnemySpawnPlanV2(EnemyActor enemyPrefab, int count)
        {
            EnemyPrefab = enemyPrefab ?? throw new ArgumentNullException(nameof(enemyPrefab));
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Enemy count must be greater than zero.");
            }

            Count = count;
        }

        public EnemyActor EnemyPrefab { get; }
        public int Count { get; }
    }
}
