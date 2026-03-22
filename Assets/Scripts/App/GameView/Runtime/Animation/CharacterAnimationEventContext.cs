using UnityEngine;

namespace Madbox.App.GameView.Animation
{
    /// <summary>
    /// Payload passed to animation event handlers after Unity invokes the router.
    /// </summary>
    public readonly struct CharacterAnimationEventContext
    {
        public CharacterAnimationEventContext(Animator sourceAnimator, AnimationEvent rawEvent)
        {
            SourceAnimator = sourceAnimator;
            RawEvent = rawEvent;
        }

        public Animator SourceAnimator { get; }

        public AnimationEvent RawEvent { get; }
    }
}
