using UnityEngine;

namespace Madbox.App.GameView.Input
{
    /// <summary>
    /// Optional override for movement input (scene wiring). Returns zero if not driving movement.
    /// </summary>
    public abstract class PlayerInputProvider : MonoBehaviour
    {
        public abstract Vector2 GetMoveDirection();
    }
}
