using System;
using UnityEngine;

namespace Madbox.Entities
{
    /// <summary>
    /// Optional hook to cancel or observe an incoming damage application (e.g. invulnerability).
    /// </summary>
    public sealed class BeforeDamageAppliedEventArgs : EventArgs
    {
        public BeforeDamageAppliedEventArgs(float amount, Vector3 attackerWorldPosition)
        {
            Amount = amount;
            AttackerWorldPosition = attackerWorldPosition;
        }

        public float Amount { get; }

        public Vector3 AttackerWorldPosition { get; }

        public bool Cancel { get; set; }
    }

    /// <summary>
    /// Raised after HP was reduced by a successful damage application.
    /// </summary>
    public sealed class DamagedEventArgs : EventArgs
    {
        public DamagedEventArgs(float amount, float newHp, Vector3 attackerWorldPosition)
        {
            Amount = amount;
            NewHp = newHp;
            AttackerWorldPosition = attackerWorldPosition;
        }

        public float Amount { get; }

        public float NewHp { get; }

        public Vector3 AttackerWorldPosition { get; }
    }

    /// <summary>
    /// Tracks local current HP against an <see cref="Entity"/>'s max-HP attribute.
    /// Attackers resolve <see cref="Damageable"/> on the target (e.g. from collision) and call <see cref="DoDamage"/>.
    /// Use <see cref="Entity"/> for target identity (type checks, <see cref="Component"/> queries on the same object).
    /// </summary>
    public sealed class Damageable : MonoBehaviour
    {
        private static readonly Func<float> DefaultNowProvider = () => Time.time;

        [SerializeField]
        private Entity entity;

        [SerializeField]
        private EntityAttribute maxHpAttribute;

        [SerializeField]
        private float currentHp;

        [SerializeField]
        private bool resetHealthInAwake = true;

        [SerializeField]
        [Min(0f)]
        private float damageDelaySeconds;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Optional delay before the owning hierarchy root is destroyed after death (e.g. enemy death animation). Read by orchestration such as EnemyService.")]
        private float destroyDelayAfterDeathSeconds;

        private float nextDamageAllowedAt;

        private Func<float> nowProvider = DefaultNowProvider;

        public event EventHandler<BeforeDamageAppliedEventArgs> BeforeDamageApplied;

        public event EventHandler<DamagedEventArgs> Damaged;

        public event EventHandler Died;

        public Entity Entity => entity;

        public float CurrentHp => currentHp;

        public bool IsAlive => currentHp > 0f;

        public float MaxHp => ComputeMaxHp();

        public float DamageDelaySeconds => damageDelaySeconds;

        public float DestroyDelayAfterDeathSeconds => destroyDelayAfterDeathSeconds;

        private float ComputeMaxHp()
        {
            if (entity == null || maxHpAttribute == null)
            {
                return 0f;
            }

            return entity.GetFloatAttribute(maxHpAttribute);
        }

        private void Awake()
        {
            if (resetHealthInAwake)
            {
                ResetToFullHealth();
            }
        }

        public void ResetToFullHealth()
        {
            float max = ComputeMaxHp();
            currentHp = Mathf.Max(0f, max);
        }

        /// <summary>
        /// Applies damage to local current HP (clamped to zero). Returns false if no damage was applied.
        /// </summary>
        public bool DoDamage(float amount)
        {
            return TryApplyDamage(amount, Vector3.zero);
        }

        /// <summary>
        /// Applies damage with an optional attacker position for knockback and presentation.
        /// </summary>
        public bool TryApplyDamage(float amount, Vector3 attackerWorldPosition)
        {
            if (amount <= 0f || currentHp <= 0f)
            {
                return false;
            }

            if (damageDelaySeconds > 0f && nowProvider() < nextDamageAllowedAt)
            {
                return false;
            }

            var before = new BeforeDamageAppliedEventArgs(amount, attackerWorldPosition);
            BeforeDamageApplied?.Invoke(this, before);
            if (before.Cancel || before.Amount <= 0f)
            {
                return false;
            }

            float appliedAmount = Mathf.Min(before.Amount, currentHp);
            currentHp = Mathf.Max(0f, currentHp - before.Amount);
            Damaged?.Invoke(this, new DamagedEventArgs(appliedAmount, currentHp, attackerWorldPosition));
            if (currentHp <= 0f)
            {
                Died?.Invoke(this, EventArgs.Empty);
            }

            if (damageDelaySeconds > 0f)
            {
                nextDamageAllowedAt = nowProvider() + damageDelaySeconds;
            }

            return true;
        }

        private void OnDisable()
        {
            nextDamageAllowedAt = 0f;
        }

        private void OnValidate()
        {
            damageDelaySeconds = Mathf.Max(0f, damageDelaySeconds);
            destroyDelayAfterDeathSeconds = Mathf.Max(0f, destroyDelayAfterDeathSeconds);
        }

        internal void SetNowProviderForTests(Func<float> provider)
        {
            nowProvider = provider ?? DefaultNowProvider;
        }
    }
}
