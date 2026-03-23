using UnityEngine;

namespace Madbox.App.Animation
{
    /// <summary>
    /// Asset marker for a clip animation event id. <see cref="EventId"/> is the asset name (rename the file to change the id).
    /// Match it to the clip event string parameter passed to <see cref="CharacterAnimationEventRouter.OnCharacterAnimationEvent(string)"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationEventDefinition", menuName = "Madbox/Animation/Animation Event Definition", order = 0)]
    public sealed class AnimationEventDefinition : ScriptableObject
    {
        /// <summary>
        /// Same as <see cref="Object.name"/>: for assets on disk, rename the asset file to change the id used in clips.
        /// </summary>
        public string EventId => name;
    }
}
