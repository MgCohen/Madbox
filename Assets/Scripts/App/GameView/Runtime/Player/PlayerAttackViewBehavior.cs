using UnityEngine;

namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Triggers attack animation when no movement input is active.
    /// </summary>
    public sealed class PlayerAttackViewBehavior : MonoBehaviour, IPlayerBehavior
    {
        [SerializeField]
        private PlayerAnimationController attackAnimation;

        [SerializeField]
        private bool enableDebugAttackInput = true;

        [SerializeField]
        private KeyCode debugAttackKey = KeyCode.Space;

        public bool TryAcceptControl(PlayerCore core)
        {
            if (core == null || core.ViewData == null || !core.ViewData.IsAlive)
            {
                return false;
            }

            if (enableDebugAttackInput && Input.GetKeyDown(debugAttackKey))
            {
                return true;
            }

            return false;
        }

        public void Execute(PlayerCore core, float deltaTime)
        {
            if (attackAnimation != null)
            {
                attackAnimation.TriggerAttack();
            }
        }
    }
}
