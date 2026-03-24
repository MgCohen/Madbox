using UnityEngine;

namespace Madbox.Entities
{
    /// <summary>
    /// Short planar knockback on <see cref="Damageable.Damaged"/>: push away from <see cref="DamagedEventArgs.AttackerWorldPosition"/>.
    /// Applies in <see cref="LateUpdate"/> so scripted movement from <see cref="EntityBehaviorRunner{TData,TInput}"/> runs first.
    /// Dynamic rigidbodies: sets horizontal velocity impulse; kinematic / no body: one-step position offset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageKnockbackReceiver : MonoBehaviour
    {
        [SerializeField]
        private Damageable damageable;

        [SerializeField]
        private Transform targetRoot;

        [SerializeField]
        private Rigidbody targetRigidbody;

        [SerializeField]
        [Min(0f)]
        private float planarDisplacement = 0.12f;

        [SerializeField]
        [Min(0f)]
        private float dynamicRigidbodyPlanarSpeed = 4f;

        private bool hasPendingKnockback;

        private Vector3 pendingAttackerWorldPosition;

        private void Awake()
        {
            if (damageable == null)
            {
                damageable = GetComponent<Damageable>() ?? GetComponentInChildren<Damageable>(true);
            }

            if (damageable == null)
            {
                return;
            }

            if (targetRoot == null)
            {
                Transform d = damageable.transform;
                if (d == transform)
                {
                    targetRoot = transform.parent != null ? transform.parent : transform;
                }
                else if (d.IsChildOf(transform))
                {
                    targetRoot = transform;
                }
                else
                {
                    targetRoot = d;
                }
            }

            if (targetRigidbody == null && targetRoot != null)
            {
                targetRigidbody = targetRoot.GetComponent<Rigidbody>();
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

            hasPendingKnockback = false;
        }

        private void OnDamaged(object sender, DamagedEventArgs e)
        {
            hasPendingKnockback = true;
            pendingAttackerWorldPosition = e.AttackerWorldPosition;
        }

        private void LateUpdate()
        {
            if (!hasPendingKnockback || targetRoot == null)
            {
                return;
            }

            hasPendingKnockback = false;
            Vector3 push = ComputePlanarPushAway(targetRoot.position, pendingAttackerWorldPosition, targetRoot.forward);

            if (ShouldUseDynamicImpulse())
            {
                ApplyDynamicImpulse(push);
            }
            else if (planarDisplacement > 0f)
            {
                targetRoot.position += push * planarDisplacement;
            }
        }

        private bool ShouldUseDynamicImpulse()
        {
            return targetRigidbody != null
                && !targetRigidbody.isKinematic
                && dynamicRigidbodyPlanarSpeed > 0f;
        }

        private void ApplyDynamicImpulse(Vector3 planarPush)
        {
            Vector3 planar = planarPush * dynamicRigidbodyPlanarSpeed;
            Vector3 v = targetRigidbody.velocity;
            targetRigidbody.velocity = new Vector3(planar.x, v.y, planar.z);
        }

        private static Vector3 ComputePlanarPushAway(Vector3 selfWorld, Vector3 attackerWorld, Vector3 fallbackForward)
        {
            Vector3 push = selfWorld - attackerWorld;
            push.y = 0f;
            if (push.sqrMagnitude < 0.0001f)
            {
                Vector3 f = fallbackForward;
                f.y = 0f;
                push = f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
            }
            else
            {
                push.Normalize();
            }

            return push;
        }
    }
}
