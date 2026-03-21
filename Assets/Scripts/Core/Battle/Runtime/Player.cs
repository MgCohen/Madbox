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
            : this(entityId, maxHealth, WeaponProfiles.LongSword)
        {
        }

        public Player(EntityId entityId, int maxHealth, WeaponProfile equippedWeapon)
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
            EquippedWeapon = equippedWeapon ?? WeaponProfiles.LongSword;
            MovementSpeed = 1f;
            IsMoving = false;
            Behaviors = new IPlayerBehaviorRuntime[] { new PlayerAutoAttackBehaviorState() };
        }

        public EntityId EntityId { get; }

        public int MaxHealth { get; }

        public int CurrentHealth { get; private set; }

        public WeaponProfile EquippedWeapon { get; private set; }

        public float MovementSpeed { get; private set; }

        public bool IsMoving { get; private set; }

        public EntityId SelectedTargetId { get; private set; }

        public float AttackRange => EquippedWeapon.Range;

        public float AttackCooldownSeconds => EquippedWeapon.CooldownSeconds;

        public float AttackTimingNormalized => EquippedWeapon.AttackTimingNormalized;

        internal IReadOnlyList<IPlayerBehaviorRuntime> Behaviors { get; }

        public void EquipWeapon(WeaponProfile weapon)
        {
            if (weapon == null) return;
            EquippedWeapon = weapon;
        }

        public void StartMoving(float speed)
        {
            if (speed < 0f) return;
            MovementSpeed = speed;
            IsMoving = true;
        }

        public void StopMoving()
        {
            IsMoving = false;
        }

        public void SelectTarget(EntityId targetId)
        {
            if (targetId == null) return;
            SelectedTargetId = targetId;
        }

        public void ClearTarget()
        {
            SelectedTargetId = null;
        }

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

