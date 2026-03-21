using System;

namespace Madbox.Levels
{
    public class LevelEnemyDefinition
    {
        public LevelEnemyDefinition(EnemyDefinition enemy, int count)
        {
            if (enemy == null)
            {
                throw new ArgumentNullException(nameof(enemy));
            }

            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Enemy count must be greater than zero.");
            }

            Enemy = enemy;
            Count = count;
        }

        public EnemyDefinition Enemy { get; }

        public int Count { get; }
    }
}

