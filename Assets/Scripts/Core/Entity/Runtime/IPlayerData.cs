using UnityEngine;

namespace Madbox.Entities
{
    /// <summary>
    /// Scene reference to the player character for enemy AI and targeting (not LiveOps persistence caches).
    /// Implemented by <see cref="Madbox.Players.Player"/>.
    /// </summary>
    public interface IPlayerData
    {
        Transform Transform { get; }
    }
}
