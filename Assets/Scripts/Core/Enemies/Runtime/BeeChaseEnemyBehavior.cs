using UnityEngine;

namespace Madbox.Enemies
{
    /// <summary>
    /// Fallback behavior: moves toward the target on the XZ plane whenever higher-priority behaviors did not claim the frame.
    /// </summary>
    public sealed class BeeChaseEnemyBehavior : IEnemyActorBehavior
    {
        public BeeChaseEnemyBehavior(float chaseSpeed, bool useRigidbodyVelocity)
        {
            this.chaseSpeed = Mathf.Max(0f, chaseSpeed);
            this.useRigidbodyVelocity = useRigidbodyVelocity;
        }

        private readonly float chaseSpeed;
        private readonly bool useRigidbodyVelocity;

        public bool TryExecute(ref EnemyBehaviorTickContext context, float deltaTime)
        {
            Transform self = context.Self;
            Transform target = context.Target;
            if (self == null || target == null || chaseSpeed <= 0f)
            {
                return false;
            }

            Vector3 toTarget = target.position - self.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return true;
            }

            Vector3 direction = toTarget / distance;
            ApplyFacing(self, direction);

            Rigidbody body = context.Body;
            if (useRigidbodyVelocity && body != null && body.isKinematic == false)
            {
                Vector3 v = body.velocity;
                Vector3 planar = direction * chaseSpeed;
                body.velocity = new Vector3(planar.x, v.y, planar.z);
            }
            else
            {
                self.position += direction * (chaseSpeed * deltaTime);
            }

            return true;
        }

        private static void ApplyFacing(Transform self, Vector3 flatForward)
        {
            self.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
        }
    }
}
