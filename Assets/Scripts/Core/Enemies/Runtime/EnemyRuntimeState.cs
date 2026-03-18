using System;
using System.Collections.Generic;
using Madbox.Enemies.Contracts;
using Madbox.Levels;
using Scaffold.MVVM;

namespace Madbox.Enemies
{
    public class EnemyRuntimeState : Model
    {
        public EnemyRuntimeState(EntityId runtimeEntityId, EnemyDefinition definition, int health, IEnemyBehaviorRuntime[] behaviors)
        {
            if (runtimeEntityId == null)
            {
                throw new ArgumentNullException(nameof(runtimeEntityId));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            RuntimeEntityId = runtimeEntityId;
            Definition = definition;
            CurrentHealth = health;
            Behaviors = behaviors ?? Array.Empty<IEnemyBehaviorRuntime>();
        }

        public EntityId RuntimeEntityId { get; }

        public EnemyDefinition Definition { get; }

        public int CurrentHealth { get; set; }

        public bool IsAlive => CurrentHealth > 0;

        public IReadOnlyList<IEnemyBehaviorRuntime> Behaviors { get; }

        public void ApplyDamage(int damage)
        {
            if (damage <= 0) return;
            CurrentHealth = Math.Max(0, CurrentHealth - damage);
        }
    }
}
