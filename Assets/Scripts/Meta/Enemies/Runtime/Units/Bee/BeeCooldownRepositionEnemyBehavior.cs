using Madbox.Entities;
using UnityEngine;

namespace Madbox.Enemies
{
    /// <summary>
    /// Medium-priority behavior: when the target is within attack range but the dash is on cooldown,
    /// slowly circles the player by picking a waypoint at a similar radius, moving toward it while
    /// facing the player. Must be registered before <see cref="BeeChaseEnemyBehavior"/> in
    /// <see cref="EnemyBehaviorRunner"/> so it outranks chase.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BeeCooldownRepositionEnemyBehavior : MonoBehaviour, IEnemyBehavior
    {
        [Header("References")]
        [SerializeField]
        private BeeDashAttackEnemyBehavior dashBehavior;

        [Header("Attributes")]
        [SerializeField]
        private EntityAttribute attackRangeAttribute;

        [SerializeField]
        private EntityAttribute chaseSpeedAttribute;

        [Header("Orbit (in range, on cooldown)")]
        [SerializeField]
        [Min(0.01f)]
        private float tooCloseDistance = 1.35f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("Multiplies orbit distance from the player when picking the next waypoint (wider circle).")]
        private float orbitRadiusMultiplier = 1.52f;

        [SerializeField]
        [Range(0.05f, 1f)]
        private float repositionSpeedMultiplier = 0.55f;

        [SerializeField]
        [Min(0.05f)]
        private float repositionRetargetSeconds = 0.48f;

        [SerializeField]
        [Tooltip("Random yaw applied to the orbit radius direction when picking the next waypoint (degrees).")]
        [Range(15f, 180f)]
        private float orbitWaypointAngleJitterDegrees = 55f;

        [SerializeField]
        private bool useRigidbodyVelocityForChase;

        private float repositionRetargetTimer;
        private Vector3 repositionMoveDirectionFlat = Vector3.forward;

        private void Awake()
        {
            if (dashBehavior == null)
            {
                dashBehavior = GetComponent<BeeDashAttackEnemyBehavior>();
            }
        }

        public bool TryAcceptControl(Enemy data, in EnemyInputContext input)
        {
            if (data == null || dashBehavior == null)
            {
                return false;
            }

            if (dashBehavior.IsDashSequenceActive)
            {
                return false;
            }

            if (dashBehavior.AttackCooldownRemaining <= 0f)
            {
                return false;
            }

            Transform self = data.transform;
            Transform target = input.PlayerData != null ? input.PlayerData.Transform : null;
            if (self == null || target == null)
            {
                return false;
            }

            float attackRange = data.GetFloatAttribute(attackRangeAttribute);
            if (attackRange <= 0f)
            {
                return false;
            }

            Vector3 toTarget = target.position - self.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance > attackRange || distance <= Mathf.Epsilon)
            {
                return false;
            }

            return true;
        }

        public void Execute(Enemy data, in EnemyInputContext input, float deltaTime)
        {
            Transform self = data != null ? data.transform : null;
            Transform target = input.PlayerData != null ? input.PlayerData.Transform : null;
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

            repositionRetargetTimer -= deltaTime;
            if (repositionRetargetTimer <= 0f)
            {
                repositionRetargetTimer = repositionRetargetSeconds;
                PickOrbitWaypoint(self, target, distance);
            }

            ApplyMoveFacePlayer(data, self, target, repositionMoveDirectionFlat, chaseSpeed * repositionSpeedMultiplier, deltaTime);
        }

        private void PickOrbitWaypoint(Transform self, Transform target, float distanceToPlayer)
        {
            Vector3 fromPlayerToSelf = self.position - target.position;
            fromPlayerToSelf.y = 0f;
            float radius = fromPlayerToSelf.magnitude;
            if (radius <= Mathf.Epsilon)
            {
                radius = 0.001f;
                fromPlayerToSelf = Vector3.forward * radius;
            }

            float targetRadius = Mathf.Max(radius, tooCloseDistance + 0.05f);
            if (distanceToPlayer < tooCloseDistance)
            {
                targetRadius = tooCloseDistance + 0.05f;
            }

            targetRadius *= orbitRadiusMultiplier;

            Vector3 radialOut = fromPlayerToSelf / radius;
            float turn = Random.Range(-orbitWaypointAngleJitterDegrees, orbitWaypointAngleJitterDegrees);
            Vector3 newRadial = Quaternion.AngleAxis(turn, Vector3.up) * radialOut;
            Vector3 orbitPoint = target.position + newRadial * targetRadius;
            Vector3 move = orbitPoint - self.position;
            move.y = 0f;
            if (move.sqrMagnitude < 0.0001f)
            {
                move = Vector3.Cross(Vector3.up, radialOut);
            }

            repositionMoveDirectionFlat = move.normalized;
        }

        private void ApplyMoveFacePlayer(Enemy data, Transform self, Transform target, Vector3 directionFlat, float speed, float deltaTime)
        {
            directionFlat.y = 0f;
            if (directionFlat.sqrMagnitude < 0.0001f)
            {
                return;
            }

            directionFlat = directionFlat.normalized;
            Vector3 toPlayer = target.position - self.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.0001f)
            {
                self.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
            }

            Rigidbody body = data != null ? data.GetComponent<Rigidbody>() : null;
            if (useRigidbodyVelocityForChase && body != null && body.isKinematic == false)
            {
                Vector3 v = body.velocity;
                Vector3 planar = directionFlat * speed;
                body.velocity = new Vector3(planar.x, v.y, planar.z);
            }
            else
            {
                self.position += directionFlat * (speed * deltaTime);
            }
        }

        public void OnQuit(Enemy data)
        {
            repositionRetargetTimer = 0f;
        }
    }
}
