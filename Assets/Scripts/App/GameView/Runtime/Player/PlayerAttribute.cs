using UnityEngine;

namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Identifies a player stat; behaviors reference this asset instead of string literals.
    /// The logical id is the Unity asset name (filename without extension), exposed via <see cref="AttributeName"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Madbox/Player/Player Attribute", fileName = "PlayerAttribute")]
    public sealed class PlayerAttribute : ScriptableObject
    {
        /// <summary>
        /// Same as <see cref="Object.name"/>: for assets on disk, rename the asset file to change this id.
        /// </summary>
        public string AttributeName => name;
    }
}
