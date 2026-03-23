using Madbox.Entities;
using UnityEngine;

namespace Madbox.Players
{
    /// <summary>
    /// Player-specific <see cref="EntityAttribute"/> asset; use for stats referenced by player view behaviors.
    /// </summary>
    [CreateAssetMenu(menuName = "Madbox/Player/Player Attribute", fileName = "PlayerAttribute")]
    public sealed class PlayerAttribute : EntityAttribute
    {
    }
}
