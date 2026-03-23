using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madbox.App.GameView.Animation
{
    /// <summary>
    /// Place on the same GameObject as the <see cref="Animator"/> that receives clip events.
    /// Clips should call <see cref="OnCharacterAnimationEvent(string)"/> with the string parameter set to the event id
    /// (matching <see cref="AnimationEventDefinition.EventId"/> on the asset).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAnimationEventRouter : MonoBehaviour
    {
        private readonly Dictionary<AnimationEventDefinition, List<Action<AnimationEventDefinition>>> handlersByDefinition =
            new Dictionary<AnimationEventDefinition, List<Action<AnimationEventDefinition>>>();

        private readonly Dictionary<string, List<AnimationEventDefinition>> definitionsByEventId =
            new Dictionary<string, List<AnimationEventDefinition>>();

        private void Awake()
        {
            if (GetComponent<Animator>() == null)
            {
                Debug.LogWarning($"{nameof(CharacterAnimationEventRouter)} on {name} requires an Animator on the same GameObject.", this);
            }
        }

        /// <summary>
        /// Registers a handler for the definition. Multiple handlers per definition are supported (multicast).
        /// </summary>
        public void Register(AnimationEventDefinition definition, Action<AnimationEventDefinition> handler)
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

            if (!handlersByDefinition.TryGetValue(definition, out List<Action<AnimationEventDefinition>> list))
            {
                list = new List<Action<AnimationEventDefinition>>(1);
                handlersByDefinition[definition] = list;
            }

            if (!list.Contains(handler))
            {
                list.Add(handler);
            }

            if (!definitionsByEventId.TryGetValue(definition.EventId, out List<AnimationEventDefinition> defs))
            {
                defs = new List<AnimationEventDefinition>(1);
                definitionsByEventId[definition.EventId] = defs;
            }

            if (!defs.Contains(definition))
            {
                defs.Add(definition);
            }
        }

        /// <summary>
        /// Removes a handler previously added with <see cref="Register"/>.
        /// </summary>
        public void Unregister(AnimationEventDefinition definition, Action<AnimationEventDefinition> handler)
        {
            if (definition == null || handler == null)
            {
                return;
            }

            if (!handlersByDefinition.TryGetValue(definition, out List<Action<AnimationEventDefinition>> list))
            {
                return;
            }

            list.Remove(handler);
            if (list.Count == 0)
            {
                handlersByDefinition.Remove(definition);
                if (definitionsByEventId.TryGetValue(definition.EventId, out List<AnimationEventDefinition> defs))
                {
                    defs.Remove(definition);
                    if (defs.Count == 0)
                    {
                        definitionsByEventId.Remove(definition.EventId);
                    }
                }
            }
        }

        /// <summary>
        /// Unity animation event callback. Configure clips to call this method with the string parameter set to the event id.
        /// </summary>
        public void OnCharacterAnimationEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogWarning($"{nameof(CharacterAnimationEventRouter)} on {name}: animation event has empty string parameter.", this);
#endif
                return;
            }

            if (!definitionsByEventId.TryGetValue(eventId, out List<AnimationEventDefinition> defs) || defs.Count == 0)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogWarning($"{nameof(CharacterAnimationEventRouter)} on {name}: no handler for animation event id '{eventId}'.", this);
#endif
                return;
            }

            for (int d = 0; d < defs.Count; d++)
            {
                AnimationEventDefinition definition = defs[d];
                if (!handlersByDefinition.TryGetValue(definition, out List<Action<AnimationEventDefinition>> handlers) || handlers.Count == 0)
                {
                    continue;
                }

                for (int i = 0; i < handlers.Count; i++)
                {
                    handlers[i]?.Invoke(definition);
                }
            }
        }
    }
}
