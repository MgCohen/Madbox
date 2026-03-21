using UnityEngine;

namespace Madbox.App.GameView
{
    public sealed class PlayerAttackViewBehavior : MonoBehaviour, IPlayerBehavior
    {
        [SerializeField] private PlayerAttackAnimationBehavior attackAnimation;
        [SerializeField] private bool enableDebugAttackInput = true;
        [SerializeField] private KeyCode debugAttackKey = KeyCode.Space;

        private void Awake()
        {
            if (attackAnimation != null) return;
            attackAnimation = GetComponent<PlayerAttackAnimationBehavior>();
            if (attackAnimation != null) return;
            attackAnimation = GetComponentInChildren<PlayerAttackAnimationBehavior>();
        }

        public bool CanTakeControl(PlayerState state, in InputContext input)
        {
            if (state.IsAlive == false) return false;
            if (input.HasJoystickInput()) return false;
            if (enableDebugAttackInput == false) return false;
            return Input.GetKeyDown(debugAttackKey);
        }

        public void OnEnterControl(PlayerState state, in InputContext input)
        {
            if (attackAnimation == null) return;
            attackAnimation.TriggerAttack();
        }

        public void Tick(PlayerState state, in InputContext input, float deltaTime)
        {
        }

        public void OnExitControl(PlayerState state, in InputContext input)
        {
        }
    }
}
