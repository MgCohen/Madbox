using UnityEngine;

namespace Madbox.App.GameView
{
    public sealed class PlayerAttackAnimationBehavior : MonoBehaviour
    {
        public bool IsAttackLocked => Time.time < attackLockedUntil;

        [SerializeField] private Animator animator;
        [SerializeField] private string attackStateName = "Weak";
        [SerializeField] private float attackLockSeconds = 0.35f;
        [SerializeField] private bool enableDebugAttackInput = true;
        [SerializeField] private KeyCode debugAttackKey = KeyCode.Space;

        private float attackLockedUntil;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void Update()
        {
            if (ShouldTriggerDebugAttack() == false) return;
            TriggerAttack();
        }

        public void TriggerAttack()
        {
            if (CanTriggerAttack() == false) return;
            animator.CrossFadeInFixedTime(attackStateName, 0.05f, 0);
            attackLockedUntil = Time.time + Mathf.Max(0f, attackLockSeconds);
        }

        private bool ShouldTriggerDebugAttack()
        {
            if (enableDebugAttackInput == false) return false;
            return Input.GetKeyDown(debugAttackKey);
        }

        private bool CanTriggerAttack()
        {
            if (animator == null) return false;
            return string.IsNullOrWhiteSpace(attackStateName) == false;
        }
    }
}
