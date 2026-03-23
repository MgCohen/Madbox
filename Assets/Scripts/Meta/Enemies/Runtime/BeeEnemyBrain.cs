using System.Collections.Generic;
using UnityEngine;

namespace Madbox.Enemies
{
    /// <summary>
    /// Bee preset: priority stack <see cref="BeeDashAttackEnemyBehavior"/> then <see cref="BeeChaseEnemyBehavior"/>,
    /// evaluated every frame via <see cref="EnemyBehaviorRunner"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BeeEnemyBrain : MonoBehaviour
    {
        [SerializeField] private Transform playerTarget;
        [SerializeField] private string playerTag = "Player";

        [Header("Attack (dash)")]
        [SerializeField, Min(0f)] private float attackRange = 4f;
        [SerializeField, Min(0f)] private float attackCooldownSeconds = 1.25f;
        [SerializeField, Min(0f)] private float dashDurationSeconds = 0.35f;
        [SerializeField, Min(0f)] private float dashSpeed = 12f;
        [SerializeField] private bool useRigidbodyImpulseForDash;
        [SerializeField, Min(0f)] private float dashImpulse = 8f;

        [Header("Chase")]
        [SerializeField, Min(0f)] private float chaseSpeed = 2.5f;
        [SerializeField] private bool useRigidbodyVelocityForChase;

        private readonly List<IEnemyActorBehavior> behaviors = new List<IEnemyActorBehavior>(2);
        private EnemyBehaviorTickContext context;
        private Rigidbody cachedBody;

        private void Awake()
        {
            cachedBody = GetComponent<Rigidbody>();
            behaviors.Clear();
            behaviors.Add(new BeeDashAttackEnemyBehavior(
                attackRange,
                attackCooldownSeconds,
                dashDurationSeconds,
                dashSpeed,
                useRigidbodyImpulseForDash,
                dashImpulse));
            behaviors.Add(new BeeChaseEnemyBehavior(chaseSpeed, useRigidbodyVelocityForChase));
        }

        private void Update()
        {
            EnemyActor actor = GetComponent<EnemyActor>();
            if (actor != null && actor.IsInitialized == false)
            {
                return;
            }

            Transform target = ResolveTarget();
            context = new EnemyBehaviorTickContext
            {
                Self = transform,
                Target = target,
                Body = cachedBody
            };

            EnemyBehaviorRunner.RunFrame(behaviors, ref context, Time.deltaTime);
        }

        private Transform ResolveTarget()
        {
            if (playerTarget != null)
            {
                return playerTarget;
            }

            if (string.IsNullOrEmpty(playerTag))
            {
                return null;
            }

            GameObject tagged = GameObject.FindGameObjectWithTag(playerTag);
            return tagged != null ? tagged.transform : null;
        }
    }
}
