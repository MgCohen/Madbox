using System;
using Madbox.Entities;
using UnityEngine;

namespace Madbox.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyDamageableDeathCleanup : MonoBehaviour
    {
        [SerializeField]
        private Damageable damageable;

        [SerializeField]
        private EnemyBehaviorRunner behaviorRunner;

        [SerializeField]
        private Rigidbody targetRigidbody;

        private void Awake()
        {
            if (damageable == null)
            {
                damageable = GetComponentInChildren<Damageable>(true);
            }

            if (behaviorRunner == null)
            {
                behaviorRunner = GetComponentInChildren<EnemyBehaviorRunner>(true);
            }

            if (targetRigidbody == null)
            {
                targetRigidbody = GetComponent<Rigidbody>();
            }
        }

        private void OnEnable()
        {
            if (damageable != null)
            {
                damageable.Died += OnDamageableDied;
            }
        }

        private void OnDisable()
        {
            if (damageable != null)
            {
                damageable.Died -= OnDamageableDied;
            }
        }

        private void OnDamageableDied(object sender, EventArgs e)
        {
            StopBehaviorsAndMotion();
        }

        private void StopBehaviorsAndMotion()
        {
            if (behaviorRunner != null)
            {
                behaviorRunner.ForceQuitActiveBehavior();
                behaviorRunner.enabled = false;
            }

            if (targetRigidbody != null && targetRigidbody.isKinematic == false)
            {
                targetRigidbody.velocity = Vector3.zero;
                targetRigidbody.angularVelocity = Vector3.zero;
            }
        }
    }
}
