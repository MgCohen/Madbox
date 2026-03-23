using UnityEngine;

namespace Madbox.Enemies
{
    /// <summary>
    /// High-priority behavior: when the target is within range and attack cooldown has elapsed,
    /// performs a short dash toward the target (transform step or optional rigidbody impulse).
    /// </summary>
    public sealed class BeeDashAttackEnemyBehavior : IEnemyActorBehavior
    {
        public BeeDashAttackEnemyBehavior(float attackRange, float attackCooldownSeconds, float dashDurationSeconds, float dashSpeed, bool useRigidbodyImpulse, float dashImpulse)
        {
            this.attackRange = Mathf.Max(0f, attackRange);
            this.attackCooldownSeconds = Mathf.Max(0f, attackCooldownSeconds);
            this.dashDurationSeconds = Mathf.Max(0f, dashDurationSeconds);
            this.dashSpeed = Mathf.Max(0f, dashSpeed);
            this.useRigidbodyImpulse = useRigidbodyImpulse;
            this.dashImpulse = Mathf.Max(0f, dashImpulse);
        }

        private readonly float attackRange;
        private readonly float attackCooldownSeconds;
        private readonly float dashDurationSeconds;
        private readonly float dashSpeed;
        private readonly bool useRigidbodyImpulse;
        private readonly float dashImpulse;

        private float attackCooldownRemaining;
        private float dashTimeRemaining;
        private Vector3 dashDirectionFlat;
        private bool impulseAppliedForCurrentDash;

        public bool TryExecute(ref EnemyBehaviorTickContext context, float deltaTime)
        {
            Transform self = context.Self;
            Transform target = context.Target;
            if (self == null || target == null)
            {
                return false;
            }

            if (dashTimeRemaining > 0f)
            {
                float dashBefore = dashTimeRemaining;
                TickDash(ref context, deltaTime);
                if (dashBefore > 0f && dashTimeRemaining <= 0f)
                {
                    attackCooldownRemaining = attackCooldownSeconds;
                }

                return true;
            }

            if (attackCooldownRemaining > 0f)
            {
                attackCooldownRemaining = Mathf.Max(0f, attackCooldownRemaining - deltaTime);
                return false;
            }

            Vector3 toTarget = target.position - self.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance > attackRange || distance <= Mathf.Epsilon)
            {
                return false;
            }

            dashDirectionFlat = toTarget / distance;
            dashTimeRemaining = dashDurationSeconds;
            impulseAppliedForCurrentDash = false;
            ApplyFacing(self, dashDirectionFlat);
            TickDash(ref context, deltaTime);
            if (dashTimeRemaining <= 0f)
            {
                attackCooldownRemaining = attackCooldownSeconds;
            }

            return true;
        }

        private void TickDash(ref EnemyBehaviorTickContext context, float deltaTime)
        {
            Transform self = context.Self;
            Rigidbody body = context.Body;

            if (useRigidbodyImpulse && body != null && body.isKinematic == false)
            {
                if (impulseAppliedForCurrentDash == false)
                {
                    body.AddForce(dashDirectionFlat * dashImpulse, ForceMode.Impulse);
                    impulseAppliedForCurrentDash = true;
                }
            }
            else if (dashSpeed > 0f)
            {
                self.position += dashDirectionFlat * (dashSpeed * deltaTime);
            }

            dashTimeRemaining = Mathf.Max(0f, dashTimeRemaining - deltaTime);
        }

        private static void ApplyFacing(Transform self, Vector3 flatForward)
        {
            if (flatForward.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            self.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
        }
    }
}
