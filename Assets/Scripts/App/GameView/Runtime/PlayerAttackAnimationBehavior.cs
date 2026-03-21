using UnityEngine;

namespace Madbox.App.GameView
{
    public sealed class PlayerAttackAnimationBehavior : MonoBehaviour
    {
        public bool IsAttackLocked => Time.time < attackLockedUntil;

        [SerializeField] private Animator animator;
        [SerializeField] private bool enableDebugAttackInput = true;
        [SerializeField] private KeyCode debugAttackKey = KeyCode.Space;
        [SerializeField] private string runStateName = "Run";
        [SerializeField] private string runFallbackStateName = "HeroMove";
        [SerializeField] private string idleStateName = "pc_fight_idle";
        [SerializeField] private string idleFallbackStateName = "HeroIdle";
        [SerializeField] private string attackStateName = "Weak";
        [SerializeField] private float attackLockSeconds = 0.35f;
        [SerializeField] private float animationCrossFadeSeconds = 0.08f;

        private float attackLockedUntil;
        private int currentStateHash;

        private void Awake()
        {
            ResolveAnimatorIfMissing();
            DisableRootMotion();
        }

        private void Update()
        {
            if (ShouldTriggerDebugAttack() == false) return;
            TriggerAttack();
        }

        public void TriggerAttack()
        {
            if (CanPlayAnimation() == false) return;
            if (string.IsNullOrWhiteSpace(attackStateName)) return;
            int attackHash = Animator.StringToHash(attackStateName);
            if (animator.HasState(0, attackHash) == false) return;
            animator.CrossFadeInFixedTime(attackHash, Mathf.Max(0f, animationCrossFadeSeconds), 0);
            currentStateHash = attackHash;
            attackLockedUntil = Time.time + Mathf.Max(0f, attackLockSeconds);
        }

        private bool ShouldTriggerDebugAttack()
        {
            if (enableDebugAttackInput == false) return false;
            return Input.GetKeyDown(debugAttackKey);
        }

        public void SetMoving(bool isMoving)
        {
            if (CanPlayAnimation() == false) return;
            if (IsAttackLocked) return;

            if (isMoving)
            {
                PlayState(runStateName, runFallbackStateName);
                return;
            }

            PlayState(idleStateName, idleFallbackStateName);
        }

        private void ResolveAnimatorIfMissing()
        {
            if (animator != null) return;
            animator = GetComponentInChildren<Animator>();
        }

        private void DisableRootMotion()
        {
            if (animator == null) return;
            animator.applyRootMotion = false;
        }

        private bool CanPlayAnimation()
        {
            if (animator == null)
            {
                ResolveAnimatorIfMissing();
                DisableRootMotion();
            }

            return animator != null;
        }

        private void PlayState(string stateName, string fallbackStateName)
        {
            int stateHash = ResolveStateHash(stateName, fallbackStateName);
            if (stateHash == 0) return;
            if (currentStateHash == stateHash) return;
            animator.CrossFadeInFixedTime(stateHash, Mathf.Max(0f, animationCrossFadeSeconds), 0);
            currentStateHash = stateHash;
        }

        private int ResolveStateHash(string stateName, string fallbackStateName)
        {
            int stateHash = ResolveStateHash(stateName);
            if (stateHash != 0) return stateHash;
            return ResolveStateHash(fallbackStateName);
        }

        private int ResolveStateHash(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName)) return 0;
            int hash = Animator.StringToHash(stateName);
            if (animator.HasState(0, hash) == false) return 0;
            return hash;
        }
    }
}
