using System;
using Madbox.App.GameView.Animation;
using UnityEngine;

namespace Madbox.App.GameView.Combat
{
    /// <summary>
    /// Registers for a <see cref="AnimationEventDefinition"/> on a nearby <see cref="CharacterAnimationEventRouter"/>
    /// (typically on the same GameObject as the <see cref="Animator"/>) and spawns a projectile at an optional origin.
    /// </summary>
    public sealed class CombatAnimationEventResponse : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;

        [SerializeField]
        private AnimationEventDefinition releaseEvent;

        [SerializeField]
        private GameObject projectilePrefab;

        [SerializeField]
        private Transform spawnOrigin;

        private CharacterAnimationEventRouter router;

        private readonly Action<CharacterAnimationEventContext> handler;

        public CombatAnimationEventResponse()
        {
            handler = OnReleaseAnimationEvent;
        }

        private void OnEnable()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null || releaseEvent == null)
            {
                return;
            }

            router = animator.GetComponent<CharacterAnimationEventRouter>();
            if (router == null)
            {
                Debug.LogWarning($"{nameof(CombatAnimationEventResponse)} on {name}: no {nameof(CharacterAnimationEventRouter)} on Animator object '{animator.name}'.", this);
                return;
            }

            router.Register(releaseEvent, handler);
        }

        private void OnDisable()
        {
            if (router != null && releaseEvent != null)
            {
                router.Unregister(releaseEvent, handler);
            }

            router = null;
        }

        private void OnReleaseAnimationEvent(CharacterAnimationEventContext context)
        {
            if (projectilePrefab == null)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"{nameof(CombatAnimationEventResponse)}: release event '{releaseEvent.DisplayName}' (no projectile prefab assigned).", this);
#endif
                return;
            }

            Transform origin = spawnOrigin != null ? spawnOrigin : animator != null ? animator.transform : transform;
            Transform facing = animator != null ? animator.transform : transform;
            Instantiate(projectilePrefab, origin.position, facing.rotation);
        }
    }
}
