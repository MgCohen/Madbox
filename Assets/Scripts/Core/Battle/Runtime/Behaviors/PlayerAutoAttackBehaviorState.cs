using System;

namespace Madbox.Battle.Behaviors
{
    internal sealed class PlayerAutoAttackBehaviorState : IPlayerBehaviorRuntime
    {
        private float cooldownRemainingSeconds;

        public bool CanAttack()
        {
            return cooldownRemainingSeconds <= 0f;
        }

        public void Tick(float deltaTime)
        {
            if (EnsureTick(deltaTime) == false) return;
            if (cooldownRemainingSeconds <= 0f) return;
            cooldownRemainingSeconds = Math.Max(0f, cooldownRemainingSeconds - deltaTime);
        }

        public void StartCooldown(float cooldownSeconds)
        {
            if (EnsureCooldown(cooldownSeconds) == false) return;
            cooldownRemainingSeconds = cooldownSeconds;
        }

        private bool EnsureTick(float deltaTime)
        {
            return deltaTime > 0f;
        }

        private bool EnsureCooldown(float cooldownSeconds)
        {
            return cooldownSeconds >= 0f;
        }
    }
}

