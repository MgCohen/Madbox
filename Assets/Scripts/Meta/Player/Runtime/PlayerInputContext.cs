using UnityEngine;

namespace Madbox.Players
{
    /// <summary>
    /// Snapshot of player input for one frame, produced by input providers.
    /// </summary>
    public readonly struct PlayerInputContext
    {
        public readonly Vector2 MoveDirection;

        public PlayerInputContext(Vector2 moveDirection)
        {
            MoveDirection = moveDirection;
        }
    }
}
