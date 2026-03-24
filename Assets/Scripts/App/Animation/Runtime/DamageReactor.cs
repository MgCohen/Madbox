using Madbox.Entities;
using UnityEngine;

namespace Madbox.App.Animation
{
    /// <summary>
    /// Subscribes to <see cref="Damageable.Damaged"/> and cross-fades to a hurt or death animator state from the same event (checks <see cref="Damageable.IsAlive"/> after the hit).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageReactor : MonoBehaviour
    {
        [SerializeField]
        private Damageable damageable;

        [SerializeField]
        private AnimationController animationController;

        [SerializeField]
        private string damagedStateName = "Damage";

        [SerializeField]
        private string deathStateName = "Die";

        [SerializeField]
        [Tooltip("When false, hit reactions skip the hurt state (death still plays when HP reaches zero).")]
        private bool playDamagedStateOnHit = true;

        private void Awake()
        {
            if (damageable == null)
            {
                damageable = GetComponentInChildren<Damageable>(true);
            }

            if (animationController == null)
            {
                animationController = GetComponentInChildren<AnimationController>(true);
            }

            if (animationController == null && transform.root != null)
            {
                animationController = transform.root.GetComponentInChildren<AnimationController>(true);
            }
        }

        private void OnEnable()
        {
            if (damageable != null)
            {
                damageable.Damaged += OnDamaged;
            }
        }

        private void OnDisable()
        {
            if (damageable != null)
            {
                damageable.Damaged -= OnDamaged;
            }
        }

        private void OnDamaged(object sender, DamagedEventArgs _)
        {
            if (damageable == null)
            {
                return;
            }

            if (animationController == null)
            {
                return;
            }

            if (!damageable.IsAlive)
            {
                if (!string.IsNullOrEmpty(deathStateName))
                {
                    animationController.Play(deathStateName);
                }

                return;
            }

            if (playDamagedStateOnHit && !string.IsNullOrEmpty(damagedStateName))
            {
                animationController.Play(damagedStateName);
            }
        }
    }
}
