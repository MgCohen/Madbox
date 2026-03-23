using Madbox.App.Animation;
using Madbox.Enemies;
using PlayerAttribute = Madbox.Players.PlayerAttribute;
using UnityEngine;

namespace Madbox.App.GameView.Players
{
    /// <summary>
    /// Overlap sphere using attack range for colliders with <see cref="Enemy"/>; drives the attack animator bool while a target is held.
    /// Facing runs in <see cref="Execute"/> while a target is held.
    /// </summary>
    public sealed class PlayerAttackViewBehavior : MonoBehaviour, IPlayerBehavior
    {
        [SerializeField]
        private AnimationController animationController;

        [SerializeField]
        private AnimationAttribute attackingParameter;

        [SerializeField]
        private PlayerAttribute attackRangeAttribute;

        [SerializeField]
        private PlayerAttribute isAliveAttribute;

        [SerializeField]
        private LayerMask enemyLayers = ~0;

        [SerializeField]
        private float rayOriginHeight = 0.5f;

        private Transform currentAttackTarget;

        private void Awake()
        {
            if (animationController == null)
            {
                animationController = GetComponentInChildren<AnimationController>(true);
            }
        }

        public bool TryAcceptControl(Madbox.Players.Player data, in PlayerInputContext _)
        {
            if (data == null || !IsEnabled(data, isAliveAttribute))
            {
                return false;
            }

            float range = data.GetFloatAttribute(attackRangeAttribute);
            if (range <= 0f)
            {
                return false;
            }

            if (currentAttackTarget != null)
            {
                // TODO: When enemy health/death exists, reject dead targets here.
                if (!IsInHorizontalRange(currentAttackTarget, range))
                {
                    currentAttackTarget = null;
                }
                else
                {
                    SetAttacking(true);
                    return true;
                }
            }

            if (TryAcquireEnemyByOverlapSphere(range, out Transform enemy))
            {
                currentAttackTarget = enemy;
                SetAttacking(true);
                return true;
            }

            return false;
        }

        private static bool IsEnabled(Madbox.Players.Player data, PlayerAttribute attribute)
        {
            if (attribute == null)
            {
                return true;
            }

            return data.GetBoolAttribute(attribute);
        }

        public void Execute(Madbox.Players.Player data, in PlayerInputContext _, float deltaTime)
        {
            if (currentAttackTarget != null)
            {
                FaceToward(currentAttackTarget);
            }
        }

        public void OnQuit(Madbox.Players.Player data)
        {
            currentAttackTarget = null;
            SetAttacking(false);
        }

        private void SetAttacking(bool value)
        {
            if (animationController != null && attackingParameter != null)
            {
                animationController.SetBool(attackingParameter, value);
            }
        }

        private void FaceToward(Transform target)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private bool IsInHorizontalRange(Transform target, float range)
        {
            if (target == null)
            {
                return false;
            }

            Vector3 delta = target.position - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= range * range;
        }

        private Vector3 RayOrigin => transform.position + Vector3.up * rayOriginHeight;

        private bool TryAcquireEnemyByOverlapSphere(float range, out Transform enemy)
        {
            enemy = null;
            Vector3 origin = RayOrigin;
            Collider[] hits = Physics.OverlapSphere(origin, range, enemyLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i];
                if (c == null || c.GetComponent<Enemy>() == null)
                {
                    continue;
                }

                enemy = c.transform;
                return true;
            }

            return false;
        }
    }
}
