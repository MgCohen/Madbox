using DG.Tweening;
using Madbox.Entities;
using UnityEngine;

namespace Madbox.Enemies
{
    /// <summary>
    /// High-priority behavior: when the target is within range and attack cooldown has elapsed,
    /// plays an optional prep scale bump, then runs a DOTween move for the dash distance over
    /// <see cref="dashDurationSecondsAttribute"/> with ease-out deceleration at the end. Normal <see cref="EnemyPlayerContactDamage"/> is
    /// disabled for the sequence; dash hit damage uses <see cref="dashDamageAttribute"/> and is
    /// applied at most once per dash when a trigger overlap with the player is reported (see Unity
    /// <c>OnTriggerEnter</c> / <c>OnTriggerStay</c>). Requires a trigger collider on this hierarchy.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BeeDashAttackEnemyBehavior : MonoBehaviour, IEnemyBehavior
    {
        [Header("Attack (dash)")]
        [SerializeField]
        private EntityAttribute attackRangeAttribute;

        [SerializeField]
        private EntityAttribute attackCooldownSecondsAttribute;

        [SerializeField]
        private EntityAttribute dashDurationSecondsAttribute;

        [SerializeField]
        private EntityAttribute dashSpeedAttribute;

        [SerializeField]
        private EntityAttribute dashDamageAttribute;

        [SerializeField]
        [Tooltip("Seconds of scale punch on dashVisual before movement. Zero skips prep.")]
        private float dashPrepSeconds = 0.4f;

        [SerializeField]
        [Min(1f)]
        private float prepScaleMultiplier = 1.5f;

        [SerializeField]
        [Tooltip("Scaled during prep. When null, defaults to a direct child named \"Bee\" or \"Presentation\" under the Enemy root, then the Enemy root.")]
        private Transform dashVisual;

        [SerializeField]
        private EnemyPlayerContactDamage contactDamage;

        [SerializeField]
        [Tooltip("Layers that count as the player for dash hit triggers. Leave empty (Nothing) to allow any layer.")]
        private LayerMask playerLayers;

        private float attackCooldownRemaining;
        private float dashPrepRemaining;
        private float dashPrepDurationStored;
        private Vector3 dashDirectionFlat;
        private Vector3 dashVisualBaseScale;
        private Rigidbody cachedRigidbody;
        private Enemy cachedEnemy;
        private bool dashDamageAppliedThisDash;
        private Tween dashMoveTween;

        /// <summary>
        /// Remaining seconds before another dash can start. Used by <see cref="BeeCooldownRepositionEnemyBehavior"/>.
        /// </summary>
        public float AttackCooldownRemaining => attackCooldownRemaining;

        /// <summary>
        /// True during prep wind-up or while the dash tween is moving the enemy.
        /// </summary>
        public bool IsDashSequenceActive => dashPrepRemaining > 0f || IsDashMoveTweenRunning();

        private void Awake()
        {
            if (dashVisual == null)
            {
                dashVisual = ResolveDefaultDashVisual(transform);
            }

            dashVisualBaseScale = dashVisual != null ? dashVisual.localScale : Vector3.one;

            if (contactDamage == null)
            {
                contactDamage = GetComponent<EnemyPlayerContactDamage>();
            }

            if (contactDamage == null)
            {
                contactDamage = GetComponentInParent<EnemyPlayerContactDamage>();
            }

            cachedRigidbody = GetComponent<Rigidbody>();
            cachedEnemy = GetComponent<Enemy>();
        }

        /// <summary>
        /// When <see cref="dashVisual"/> is unset, the behaviour often lives on an empty child (e.g. "Behaviours");
        /// scaling only that node is invisible. Prefer the mesh holder under the entity root.
        /// </summary>
        private static Transform ResolveDefaultDashVisual(Transform behaviourTransform)
        {
            Enemy enemy = behaviourTransform.GetComponentInParent<Enemy>();
            Transform root = enemy != null ? enemy.transform : behaviourTransform.root;

            Transform bee = root.Find("Bee");
            if (bee != null)
            {
                return bee;
            }

            Transform presentation = root.Find("Presentation");
            if (presentation != null)
            {
                return presentation;
            }

            if (enemy != null)
            {
                return root;
            }

            return behaviourTransform;
        }

        public bool TryAcceptControl(Enemy data, in EnemyInputContext input)
        {
            if (data == null)
            {
                return false;
            }

            float dt = Time.deltaTime;
            bool inDashSequence = dashPrepRemaining > 0f || IsDashMoveTweenRunning();
            if (!inDashSequence && attackCooldownRemaining > 0f)
            {
                attackCooldownRemaining = Mathf.Max(0f, attackCooldownRemaining - dt);
            }

            Transform self = data.transform;
            Transform target = input.PlayerData != null ? input.PlayerData.Transform : null;
            if (self == null)
            {
                return false;
            }

            if (inDashSequence)
            {
                return true;
            }

            if (target == null || attackCooldownRemaining > 0f)
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
            if (self == null)
            {
                return;
            }

            if (dashPrepRemaining > 0f)
            {
                dashPrepRemaining -= deltaTime;
                UpdatePrepScale();
                if (dashPrepRemaining <= 0f)
                {
                    dashPrepRemaining = 0f;
                    ResetPrepScale();
                    BeginDashMovement(data, self);
                }

                return;
            }

            if (IsDashMoveTweenRunning())
            {
                return;
            }

            if (target == null)
            {
                return;
            }

            float range = data != null ? data.GetFloatAttribute(attackRangeAttribute) : 0f;
            if (range <= 0f)
            {
                return;
            }

            Vector3 toTarget = target.position - self.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance > range || distance <= Mathf.Epsilon)
            {
                return;
            }

            dashDirectionFlat = toTarget / distance;
            if (dashDirectionFlat.sqrMagnitude > Mathf.Epsilon)
            {
                self.rotation = Quaternion.LookRotation(dashDirectionFlat, Vector3.up);
            }

            SetContactDamageEnabled(false);
            dashPrepDurationStored = Mathf.Max(0f, dashPrepSeconds);
            dashPrepRemaining = dashPrepDurationStored;
            if (dashPrepRemaining > 0f)
            {
                return;
            }

            BeginDashMovement(data, self);
        }

        private void BeginDashMovement(Enemy data, Transform self)
        {
            KillDashMoveTween();
            dashDamageAppliedThisDash = false;
            float duration = data != null ? data.GetFloatAttribute(dashDurationSecondsAttribute) : 0f;
            float dashSpeed = data != null ? data.GetFloatAttribute(dashSpeedAttribute) : 0f;
            duration = Mathf.Max(0f, duration);
            float travelDistance = dashSpeed * duration;
            Vector3 dashEndPosition = self.position + dashDirectionFlat * travelDistance;
            Enemy enemyForCallback = data;

            if (duration <= Mathf.Epsilon)
            {
                ApplyDashPosition(self, dashEndPosition);
                dashMoveTween = null;
                OnDashEnded(enemyForCallback);
                return;
            }

            dashMoveTween = DOTween
                .To(() => self.position, p => ApplyDashPosition(self, p), dashEndPosition, duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(UpdateType.Normal, false)
                .OnComplete(() =>
                {
                    dashMoveTween = null;
                    OnDashEnded(enemyForCallback);
                });
        }

        private bool IsDashMoveTweenRunning()
        {
            return dashMoveTween != null && dashMoveTween.IsActive();
        }

        private void KillDashMoveTween()
        {
            if (dashMoveTween != null && dashMoveTween.IsActive())
            {
                dashMoveTween.Kill();
            }

            dashMoveTween = null;
        }

        private void ApplyDashPosition(Transform self, Vector3 position)
        {
            Rigidbody body = cachedRigidbody;
            if (body != null)
            {
                body.MovePosition(position);
            }
            else
            {
                self.position = position;
            }
        }

        private void OnDashEnded(Enemy data)
        {
            float cooldown = data != null ? data.GetFloatAttribute(attackCooldownSecondsAttribute) : 0f;
            attackCooldownRemaining = Mathf.Max(0f, cooldown);
            SetContactDamageEnabled(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryApplyDashDamageFromPlayerTrigger(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryApplyDashDamageFromPlayerTrigger(other);
        }

        /// <summary>
        /// Forward trigger callbacks from a child GameObject that holds the trigger collider; Unity only
        /// invokes <see cref="OnTriggerEnter"/> on the object with the collider.
        /// </summary>
        public void ForwardDashTrigger(Collider other)
        {
            TryApplyDashDamageFromPlayerTrigger(other);
        }

        private void TryApplyDashDamageFromPlayerTrigger(Collider other)
        {
            if (!IsDashMoveTweenRunning() || dashDamageAppliedThisDash)
            {
                return;
            }

            if (dashDamageAttribute == null || other == null || cachedEnemy == null)
            {
                return;
            }

            if (playerLayers.value != 0 && (playerLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            if (other.GetComponentInParent<IPlayerData>() == null)
            {
                return;
            }

            Damageable damageable = PlayerDamageableResolver.TryResolveFromCollider(other);
            if (damageable == null || !damageable.IsAlive)
            {
                return;
            }

            float amount = cachedEnemy.GetFloatAttribute(dashDamageAttribute);
            if (amount <= 0f)
            {
                return;
            }

            damageable.TryApplyDamage(amount, cachedEnemy.transform.position);
            dashDamageAppliedThisDash = true;
        }

        private void UpdatePrepScale()
        {
            if (dashVisual == null || dashPrepDurationStored <= Mathf.Epsilon)
            {
                return;
            }

            float u = 1f - (dashPrepRemaining / dashPrepDurationStored);
            u = Mathf.Clamp01(u);
            float bump = Mathf.Sin(Mathf.PI * u);
            float factor = Mathf.Lerp(1f, prepScaleMultiplier, bump);
            dashVisual.localScale = dashVisualBaseScale * factor;
        }

        private void ResetPrepScale()
        {
            if (dashVisual != null)
            {
                dashVisual.localScale = dashVisualBaseScale;
            }
        }

        private void SetContactDamageEnabled(bool enabled)
        {
            if (contactDamage != null)
            {
                contactDamage.enabled = enabled;
            }
        }

        public void OnQuit(Enemy data)
        {
            dashPrepRemaining = 0f;
            dashDamageAppliedThisDash = false;
            KillDashMoveTween();
            ResetPrepScale();
            SetContactDamageEnabled(true);
        }
    }
}
