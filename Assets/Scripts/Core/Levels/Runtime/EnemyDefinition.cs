using System;
using System.Collections.Generic;
using Madbox.Levels.Behaviors;

namespace Madbox.Levels
{
    public class EnemyDefinition
    {
        public EnemyDefinition(EntityId enemyTypeId, int maxHealth, IReadOnlyList<EnemyBehaviorDefinition> behaviors)
        {
            if (enemyTypeId == null)
            {
                throw new ArgumentNullException(nameof(enemyTypeId));
            }

            if (string.IsNullOrWhiteSpace(enemyTypeId.Value))
            {
                throw new ArgumentException("Enemy type id is required.", nameof(enemyTypeId));
            }

            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), "Max health must be greater than zero.");
            }

            if (behaviors == null)
            {
                throw new ArgumentNullException(nameof(behaviors));
            }

            if (behaviors.Count == 0)
            {
                throw new ArgumentException("At least one behavior is required.", nameof(behaviors));
            }

            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] == null)
                {
                    throw new ArgumentException("Behavior entries cannot be null.", nameof(behaviors));
                }
            }

            EnemyTypeId = enemyTypeId;
            MaxHealth = maxHealth;
            Behaviors = behaviors;
        }

        public EntityId EnemyTypeId { get; }

        public int MaxHealth { get; }

        public IReadOnlyList<EnemyBehaviorDefinition> Behaviors { get; }
    }
}
