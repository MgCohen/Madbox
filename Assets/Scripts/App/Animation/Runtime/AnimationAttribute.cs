using UnityEngine;

namespace Madbox.App.Animation
{
    /// <summary>
    /// Identifies an animator parameter; behaviors and <see cref="AnimationController"/> reference this asset instead of string literals.
    /// The parameter name matches the Unity asset name (filename without extension), exposed via <see cref="ParameterName"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Madbox/Animation/Animation Attribute", fileName = "AnimationAttribute")]
    public sealed class AnimationAttribute : ScriptableObject
    {
        /// <summary>
        /// Same as <see cref="Object.name"/>: must match the Animator Controller parameter name; rename the asset file to change it.
        /// </summary>
        public string ParameterName => name;
    }
}
