using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madbox.App.GameView.Animation
{
    /// <summary>
    /// Place on the same GameObject as the <see cref="Animator"/> that receives clip events.
    /// Clip events should call <see cref="OnCharacterAnimationEvent"/> and pass the stable id in intParameter.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAnimationEventRouter : MonoBehaviour
    {
        private readonly Dictionary<int, List<Action<CharacterAnimationEventContext>>> handlersById = new Dictionary<int, List<Action<CharacterAnimationEventContext>>>();

        private Animator cachedAnimator;

        private void Awake()
        {
            cachedAnimator = GetComponent<Animator>();
            if (cachedAnimator == null)
            {
                Debug.LogWarning($"{nameof(CharacterAnimationEventRouter)} on {name} requires an Animator on the same GameObject.", this);
            }
        }

        /// <summary>
        /// Registers a handler for the definition's stable id. Multiple handlers per id are supported (multicast).
        /// </summary>
        public void Register(AnimationEventDefinition definition, Action<CharacterAnimationEventContext> handler)
        {
            if (definition == null || handler == null)
            {
                return;
            }

            int id = definition.StableId;
            if (id == 0)
            {
                Debug.LogWarning($"{nameof(AnimationEventDefinition)} '{definition.name}' has StableId 0; skipped.", definition);
                return;
            }

            if (!handlersById.TryGetValue(id, out List<Action<CharacterAnimationEventContext>> list))
            {
                list = new List<Action<CharacterAnimationEventContext>>(1);
                handlersById[id] = list;
            }

            if (!list.Contains(handler))
            {
                list.Add(handler);
            }
        }

        /// <summary>
        /// Removes a handler previously added with <see cref="Register"/>.
        /// </summary>
        public void Unregister(AnimationEventDefinition definition, Action<CharacterAnimationEventContext> handler)
        {
            if (definition == null || handler == null)
            {
                return;
            }

            int id = definition.StableId;
            if (!handlersById.TryGetValue(id, out List<Action<CharacterAnimationEventContext>> list))
            {
                return;
            }

            list.Remove(handler);
            if (list.Count == 0)
            {
                handlersById.Remove(id);
            }
        }

        /// <summary>
        /// Unity animation event callback. Configure clips to call this method name with intParameter = stable id.
        /// </summary>
        public void OnCharacterAnimationEvent(AnimationEvent animationEvent)
        {
            int id = animationEvent.intParameter;
            if (id == 0)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogWarning($"{nameof(CharacterAnimationEventRouter)} on {name}: animation event has intParameter 0 (unset).", this);
#endif
                return;
            }

            if (!handlersById.TryGetValue(id, out List<Action<CharacterAnimationEventContext>> list) || list.Count == 0)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogWarning($"{nameof(CharacterAnimationEventRouter)} on {name}: no handler for animation event id {id}.", this);
#endif
                return;
            }

            Animator source = cachedAnimator != null ? cachedAnimator : GetComponent<Animator>();
            var context = new CharacterAnimationEventContext(source, animationEvent);

            for (int i = 0; i < list.Count; i++)
            {
                list[i]?.Invoke(context);
            }
        }
    }
}
