using Madbox.App.Animation;
using Madbox.Enemies;
using Madbox.Entities;
using UnityEngine;

namespace Madbox.Players
{
    /// <summary>
    /// Uses <see cref="Physics.OverlapSphere"/> at attack range to find enemy colliders, then filters by horizontal distance (no facing angle or line-of-sight); drives the attack animator bool while a target is held.
    /// When several enemies overlap, the closest horizontal match is chosen. Hit events prefer the current locked target when still in range.
    /// Facing runs in <see cref="Execute"/> while a target is held.
    /// </summary>
    public sealed class PlayerAttackViewBehavior : MonoBehaviour, IPlayerBehavior
    {
        [SerializeField]
        private AnimationController animationController;

        [SerializeField]
        private AnimationAttribute attackingParameter;

        [SerializeField]
        private AnimationEventRouter animationEventRouter;

        [SerializeField]
        private AnimationEventDefinition attackHitEvent;

        [SerializeField]
        private EntityAttribute attackRangeAttribute;

        [SerializeField]
        private EntityAttribute attackDamageAttribute;

        [SerializeField]
        private EntityAttribute isAliveAttribute;

        [SerializeField]
        private LayerMask enemyLayers = ~0;

        [SerializeField]
        private float rayOriginHeight = 0.5f;

        [SerializeField]
        [Min(0f)]
        private float attackDamageWhenAttributeMissing = 1f;

        [SerializeField]
        [Tooltip("When set, this transform is rotated toward the attack target. When null, uses the Player component on a parent (typically the hero root), matching movement facing. Assign when the behavior lives on a child object (e.g. Behaviours).")]
        private Transform facingRoot;

        private Player ownerPlayer;
        private Player currentPlayerData;
        private Transform currentAttackTarget;

        private Transform facingTarget;

        private void Awake()
        {
            ownerPlayer = GetComponentInParent<Player>();

            if (animationController == null)
            {
                animationController = GetComponentInChildren<AnimationController>(true);
            }

            if (animationEventRouter == null)
            {
                animationEventRouter = GetComponentInChildren<AnimationEventRouter>(true);
            }

            if (facingRoot != null)
            {
                facingTarget = facingRoot;
            }
            else
            {
                facingTarget = ownerPlayer != null ? ownerPlayer.transform : transform;
            }
        }

        private void OnEnable()
        {
            if (animationEventRouter != null && attackHitEvent != null)
            {
                animationEventRouter.Register(attackHitEvent, OnAttackHitEvent);
            }
        }

        private void OnDisable()
        {
            if (animationEventRouter != null && attackHitEvent != null)
            {
                animationEventRouter.Unregister(attackHitEvent, OnAttackHitEvent);
            }

            currentPlayerData = null;
            currentAttackTarget = null;
            SetAttacking(false);
        }

        public bool TryAcceptControl(Player data, in PlayerInputContext _)
        {
            if (data == null || !IsEnabled(data, isAliveAttribute))
            {
                currentPlayerData = null;
                return false;
            }

            currentPlayerData = data;

            float range = data.GetFloatAttribute(attackRangeAttribute);
            if (range <= 0f)
            {
                return false;
            }

            if (currentAttackTarget != null)
            {
                if (!IsViableAttackTarget(currentAttackTarget) || !IsInHorizontalRange(currentAttackTarget, range))
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

        private static bool IsEnabled(Player data, EntityAttribute attribute)
        {
            if (attribute == null)
            {
                return true;
            }

            return data.GetBoolAttribute(attribute);
        }

        public void Execute(Player data, in PlayerInputContext _, float deltaTime)
        {
            if (currentAttackTarget == null)
            {
                return;
            }

            float range = data != null ? data.GetFloatAttribute(attackRangeAttribute) : 0f;
            if (range <= 0f || !IsViableAttackTarget(currentAttackTarget) || !IsInHorizontalRange(currentAttackTarget, range))
            {
                currentAttackTarget = null;
                SetAttacking(false);
                return;
            }

            FaceToward(currentAttackTarget);
        }

        public void OnQuit(Player data)
        {
            currentPlayerData = null;
            currentAttackTarget = null;
            SetAttacking(false);
        }

        private void OnAttackHitEvent(AnimationEventContext _)
        {
            // Use owner when the runner has already switched to another behavior this frame (OnQuit clears
            // currentPlayerData) or when animation events run after behavior Update order.
            Player data = currentPlayerData != null ? currentPlayerData : ownerPlayer;
            if (data == null || !IsEnabled(data, isAliveAttribute))
            {
                return;
            }

            float range = data.GetFloatAttribute(attackRangeAttribute);
            if (range <= 0f)
            {
                return;
            }

            Transform enemyRoot = null;
            if (currentAttackTarget != null
                && IsViableAttackTarget(currentAttackTarget)
                && IsInHorizontalRange(currentAttackTarget, range))
            {
                enemyRoot = currentAttackTarget;
            }
            else if (!TryAcquireEnemyByOverlapSphere(range, out enemyRoot))
            {
                return;
            }

            Damageable damageable = TryResolveEnemyDamageable(enemyRoot);
            if (damageable == null || !damageable.IsAlive)
            {
                return;
            }

            float damage = ResolveAttackDamage(data);
            if (damage <= 0f)
            {
                return;
            }

            damageable.TryApplyDamage(damage, facingTarget.position);
        }

        private float ResolveAttackDamage(Player data)
        {
            if (data == null || attackDamageAttribute == null)
            {
                return attackDamageWhenAttributeMissing;
            }

            return data.GetFloatAttribute(attackDamageAttribute);
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
            Transform origin = facingTarget != null ? facingTarget : transform;
            Vector3 dir = target.position - origin.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            origin.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private bool IsInHorizontalRange(Transform target, float range)
        {
            if (target == null)
            {
                return false;
            }

            Transform origin = facingTarget != null ? facingTarget : transform;
            Vector3 delta = target.position - origin.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= range * range;
        }

        private static bool IsViableAttackTarget(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            if (target.GetComponentInParent<Enemy>() == null)
            {
                return false;
            }

            Damageable d = TryResolveEnemyDamageable(target);
            return d == null || d.IsAlive;
        }

        /// <summary>
        /// Attack targeting uses the enemy root (see <see cref="TryAcquireEnemyByOverlapSphere"/>). Damageable may live on a child (e.g. a "reactors" sub-object), so we resolve from descendants, not parents.
        /// </summary>
        private static Damageable TryResolveEnemyDamageable(Transform enemyRoot)
        {
            return enemyRoot != null ? enemyRoot.GetComponentInChildren<Damageable>(true) : null;
        }

        private Vector3 RayOrigin
        {
            get
            {
                Transform t = facingTarget != null ? facingTarget : transform;
                return t.position + Vector3.up * rayOriginHeight;
            }
        }

        private bool TryAcquireEnemyByOverlapSphere(float range, out Transform enemy)
        {
            enemy = null;
            Vector3 origin = RayOrigin;
            Collider[] hits = Physics.OverlapSphere(origin, range, enemyLayers, QueryTriggerInteraction.Collide);
            float bestSqrHorizontal = float.MaxValue;
            Transform bestEnemy = null;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i];
                if (c == null)
                {
                    continue;
                }

                Enemy enemyData = c.GetComponentInParent<Enemy>();
                if (enemyData == null)
                {
                    continue;
                }

                if (!IsViableAttackTarget(enemyData.transform))
                {
                    continue;
                }

                // Same horizontal limit as retention and hit validation; overlap alone can still hit large colliders when the enemy root is past range.
                if (!IsInHorizontalRange(enemyData.transform, range))
                {
                    continue;
                }

                Transform t = enemyData.transform;
                float sqr = HorizontalDeltaSqrFromFacingOrigin(t);
                if (sqr < bestSqrHorizontal)
                {
                    bestSqrHorizontal = sqr;
                    bestEnemy = t;
                }
            }

            enemy = bestEnemy;
            return enemy != null;
        }

        private float HorizontalDeltaSqrFromFacingOrigin(Transform target)
        {
            Transform origin = facingTarget != null ? facingTarget : transform;
            Vector3 delta = target.position - origin.position;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }
    }
}
