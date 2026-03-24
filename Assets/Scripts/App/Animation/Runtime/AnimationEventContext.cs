using UnityEngine;

namespace Madbox.App.Animation
{
    /// <summary>
    /// Payload for clip events handled by <see cref="AnimationEventRouter"/>.
    /// </summary>
    public readonly struct AnimationEventContext
    {
        public AnimationEventContext(Animator animator, AnimationEventDefinition definition)
        {
            Animator = animator;
            Definition = definition;
        }

        /// <summary>
        /// The <see cref="Animator"/> on the same GameObject as the <see cref="AnimationEventRouter"/> that received the callback.
        /// </summary>
        public Animator Animator { get; }

        /// <summary>
        /// The <see cref="AnimationEventDefinition"/> asset passed from the clip (object reference).
        /// </summary>
        public AnimationEventDefinition Definition { get; }

        /// <summary>
        /// Convenience: same as <see cref="AnimationEventDefinition.EventId"/> when <see cref="Definition"/> is set.
        /// </summary>
        public string EventId => Definition != null ? Definition.EventId : string.Empty;
    }
}
