using UnityEngine;

namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Crossfade-based locomotion and attack states; applies <see cref="AttackSpeedMultiplierParameter"/> from <see cref="PlayerViewData.AttackSpeedStat"/>.
    /// </summary>
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        public const string AttackSpeedMultiplierParameter = "AttackSpeedMultiplier";

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private PlayerViewData statSource;

        [SerializeField]
        private string idleStateName = "pc_fight_idle";

        [SerializeField]
        private string runStateName = "Run";

        [SerializeField]
        private string attackStateName = "Weak";

        [SerializeField]
        private float attackLockSeconds = 0.35f;

        private int attackSpeedHash;

        private float attackLockedUntil;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (statSource == null)
            {
                statSource = GetComponentInParent<PlayerViewData>();
            }

            attackSpeedHash = Animator.StringToHash(AttackSpeedMultiplierParameter);
        }

        private void Update()
        {
            if (animator == null || statSource == null)
            {
                return;
            }

            animator.SetFloat(attackSpeedHash, statSource.AttackSpeedStat);
        }

        public void SetLocomotionMoving(bool moving)
        {
            if (animator == null || Time.time < attackLockedUntil)
            {
                return;
            }

            string target = moving ? runStateName : idleStateName;
            animator.CrossFade(target, 0.1f, 0);
        }

        public void TriggerAttack()
        {
            if (animator == null)
            {
                return;
            }

            animator.CrossFade(attackStateName, 0.05f, 0);
            attackLockedUntil = Time.time + attackLockSeconds;
        }
    }
}
