using UnityEngine;

namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Snapshot of player input for one frame, produced by <see cref="Madbox.App.GameView.Input.PlayerInputProvider"/>.
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
