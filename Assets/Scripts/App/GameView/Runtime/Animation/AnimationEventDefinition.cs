using UnityEngine;

namespace Madbox.App.GameView.Animation
{
    /// <summary>
    /// Asset marker for a clip animation event id. Match <see cref="EventId"/> to the clip event string parameter.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationEventDefinition", menuName = "Madbox/Game View/Animation Event Definition", order = 0)]
    public sealed class AnimationEventDefinition : ScriptableObject
    {
        [SerializeField]
        private string eventId;

        /// <summary>
        /// String dispatched from clips via <see cref="CharacterAnimationEventRouter.OnCharacterAnimationEvent(string)"/>.
        /// </summary>
        public string EventId => string.IsNullOrEmpty(eventId) ? name : eventId;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(eventId))
            {
                eventId = name;
            }
        }
#endif
    }
}
