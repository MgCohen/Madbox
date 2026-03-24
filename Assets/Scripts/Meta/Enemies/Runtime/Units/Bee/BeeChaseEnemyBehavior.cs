using Madbox.Entities;
using UnityEngine;

namespace Madbox.Enemies
{
    /// <summary>
    /// Fallback behavior: moves toward the target on the XZ plane whenever higher-priority behaviors did not claim the frame.
    /// Chase speed is read from <see cref="Enemy"/> via <see cref="EntityAttribute"/> (same entity-attribute pattern as player behaviors).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BeeChaseEnemyBehavior : MonoBehaviour, IEnemyBehavior
    {
        [Header("Chase")]
        [SerializeField]
        private EntityAttribute chaseSpeedAttribute;

        [SerializeField]
        private bool useRigidbodyVelocityForChase;

        public bool TryAcceptControl(Enemy data, in EnemyInputContext input)
        {
            Transform target = GetPlayerTransform(in input);
            float chaseSpeed = data != null ? data.GetFloatAttribute(chaseSpeedAttribute) : 0f;
            return target != null && chaseSpeed > 0f;
        }

        public void Execute(Enemy data, in EnemyInputContext input, float deltaTime)
        {
            Transform self = data != null ? data.transform : null;
            Transform target = GetPlayerTransform(in input);
            float chaseSpeed = data != null ? data.GetFloatAttribute(chaseSpeedAttribute) : 0f;
            if (self == null || target == null || chaseSpeed <= 0f)
            {
                return;
            }

            Vector3 toTarget = target.position - self.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return;
            }

            Vector3 direction = toTarget / distance;
            ApplyFacing(self, direction);

            Rigidbody body = data != null ? data.GetComponent<Rigidbody>() : null;
            if (useRigidbodyVelocityForChase && body != null && body.isKinematic == false)
            {
                Vector3 v = body.velocity;
                Vector3 planar = direction * chaseSpeed;
                body.velocity = new Vector3(planar.x, v.y, planar.z);
            }
            else
            {
                self.position += direction * (chaseSpeed * deltaTime);
            }
        }

        public void OnQuit(Enemy data)
        {
        }

        private static void ApplyFacing(Transform self, Vector3 flatForward)
        {
            self.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
        }

        private static Transform GetPlayerTransform(in EnemyInputContext input)
        {
            return input.PlayerData != null ? input.PlayerData.Transform : null;
        }
    }
}
