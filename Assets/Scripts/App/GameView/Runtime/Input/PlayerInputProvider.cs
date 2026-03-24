using Madbox.Entities;
using Madbox.Players;
using UnityEngine;

namespace Madbox.App.GameView.Input
{
    /// <summary>
    /// Resolves <see cref="PlayerInputContext"/> for the current frame (wired on the scene or player prefab).
    /// </summary>
    public abstract class PlayerInputProvider : MonoBehaviour, IEntityFrameInputProvider<PlayerInputContext>
    {
        public abstract PlayerInputContext GetInputContext();

        PlayerInputContext IEntityFrameInputProvider<PlayerInputContext>.GetFrameInput()
        {
            return GetInputContext();
        }
    }
}
