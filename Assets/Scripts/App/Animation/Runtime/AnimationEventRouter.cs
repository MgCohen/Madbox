using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madbox.App.Animation
{
    /// <summary>
    /// Place on the same GameObject as the <see cref="Animator"/> that receives clip events.
    /// Clips should call <see cref="OnCharacterAnimationEvent(Object)"/> with the animation event Object field set to the
    /// <see cref="AnimationEventDefinition"/> asset (Unity delivers <see cref="Object"/>; use <see cref="OnCharacterAnimationEvent(AnimationEventDefinition)"/> from code).
    /// Subscribe with <see cref="Register"/> / <see cref="Unregister"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnimationEventRouter : MonoBehaviour
    {
        private Animator _animator;

        private readonly Dictionary<AnimationEventDefinition, List<Action<AnimationEventContext>>> handlersByDefinition =
            new Dictionary<AnimationEventDefinition, List<Action<AnimationEventContext>>>();

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogWarning($"{nameof(AnimationEventRouter)} on {name} requires an Animator on the same GameObject.", this);
            }
        }

        public void Register(AnimationEventDefinition definition, Action<AnimationEventContext> handler)
        {
            if (definition == null || handler == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(definition.EventId))
            {
                Debug.LogWarning($"{nameof(AnimationEventDefinition)} '{definition.name}' has empty EventId; skipped.", definition);
                return;
            }

            if (!handlersByDefinition.TryGetValue(definition, out List<Action<AnimationEventContext>> list))
            {
                list = new List<Action<AnimationEventContext>>(1);
                handlersByDefinition[definition] = list;
            }

            if (!list.Contains(handler))
            {
                list.Add(handler);
            }
        }

        public void Unregister(AnimationEventDefinition definition, Action<AnimationEventContext> handler)
        {
            if (definition == null || handler == null)
            {
                return;
            }

            if (!handlersByDefinition.TryGetValue(definition, out List<Action<AnimationEventContext>> list))
            {
                return;
            }

            list.Remove(handler);
            if (list.Count == 0)
            {
                handlersByDefinition.Remove(definition);
            }
        }

        public void OnCharacterAnimationEvent(AnimationEventDefinition definition)
        {
            if (definition == null)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogWarning($"{nameof(AnimationEventRouter)} on {name}: animation event has no object reference.", this);
#endif
                return;
            }

            if (!handlersByDefinition.TryGetValue(definition, out List<Action<AnimationEventContext>> handlers) || handlers.Count == 0)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogWarning($"{nameof(AnimationEventRouter)} on {name}: no handler for animation event '{definition.EventId}'.", this);
#endif
                return;
            }

            Animator animator = _animator != null ? _animator : GetComponent<Animator>();
            var context = new AnimationEventContext(animator, definition);

            for (int i = 0; i < handlers.Count; i++)
            {
                handlers[i]?.Invoke(context);
            }
        }
    }
}
