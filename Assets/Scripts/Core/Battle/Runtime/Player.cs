using System;
using Madbox.Levels;
#pragma warning disable SCA0017

namespace Madbox.Battle
{
    internal class Player
    {
        public Player(EntityId entityId, int maxHealth)
        {
            EntityId = entityId ?? throw new ArgumentNullException(nameof(entityId));
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), "Player max health must be greater than zero.");
            }

            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public EntityId EntityId { get; }

        public int MaxHealth { get; }

        public int CurrentHealth { get; private set; }

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
