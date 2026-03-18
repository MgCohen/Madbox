using System;
using System.Collections.Generic;
using Madbox.Battle.Behaviors;
using Madbox.Levels;
using Scaffold.MVVM;

namespace Madbox.Battle
{
    public class Player : Model
    {
        public Player(EntityId entityId, int maxHealth)
        {
            if (entityId == null)
            {
                throw new ArgumentNullException(nameof(entityId));
            }

            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), "Player max health must be greater than zero.");
            }

            EntityId = entityId;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            Behaviors = new IPlayerBehaviorRuntime[] { new PlayerAutoAttackBehaviorState() };
        }

        public EntityId EntityId { get; }

        public int MaxHealth { get; }

        public int CurrentHealth { get; private set; }

        internal IReadOnlyList<IPlayerBehaviorRuntime> Behaviors { get; }

        public int ApplyDamage(int damage)
        {
            if (damage <= 0)
            {
                return CurrentHealth;
            }

            CurrentHealth = Math.Max(0, CurrentHealth - damage);
            return CurrentHealth;
        }
    }
}
