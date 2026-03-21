using System;
using Madbox.Enemies.Contracts;
using Madbox.Levels.Behaviors;

namespace Madbox.Enemies.Behaviors
{
    public sealed class ContactAttackBehaviorRuntime : IEnemyBehaviorRuntime
    {
        public ContactAttackBehaviorRuntime(ContactAttackBehaviorDefinition definition)
        {
            EnsureDefinition(definition);
            this.definition = definition;
        }

        private readonly ContactAttackBehaviorDefinition definition;
        private float cooldownRemainingSeconds;

        public bool TryConsume(int rawDamage, out int appliedDamage)
        {
            if (CanConsume(rawDamage, out appliedDamage) == false) return false;
            cooldownRemainingSeconds = ResolveCooldown();
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            if (cooldownRemainingSeconds <= 0f) return;
            cooldownRemainingSeconds = Math.Max(0f, cooldownRemainingSeconds - deltaTime);
        }

        private void EnsureDefinition(ContactAttackBehaviorDefinition input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
        }

        private bool CanConsume(int rawDamage, out int appliedDamage)
        {
            appliedDamage = default;
            if (rawDamage <= 0) return false;
            if (cooldownRemainingSeconds > 0f) return false;
            appliedDamage = Math.Min(rawDamage, definition.Damage);
            return appliedDamage > 0;
        }

        private float ResolveCooldown()
        {
            return Math.Max(0f, definition.CooldownSeconds);
        }
    }
}

