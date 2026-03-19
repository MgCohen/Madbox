using UnityEngine;

namespace Madbox.App.GameView
{
    public sealed class PlayerMovementViewBehavior : MonoBehaviour
    {
        [SerializeField] private VirtualJoystickInput joystick;
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerAttackAnimationBehavior attackBehavior;
        [SerializeField] private float movementSpeed = 3.5f;
        [SerializeField] private float inputThreshold = 0.05f;
        [SerializeField] private bool invertHorizontalAxis = true;
        [SerializeField] private bool invertVerticalAxis = true;
        [SerializeField] private bool rotateToDirection = true;
        [SerializeField] private float rotateLerpSpeed = 12f;
        [SerializeField] private string runStateName = "Run";
        [SerializeField] private string runFallbackStateName = "HeroMove";
        [SerializeField] private string idleStateName = "pc_fight_idle";
        [SerializeField] private string idleFallbackStateName = "HeroIdle";

        private bool wasMoving;

        private void Awake()
        {
            ResolveJoystickIfMissing();
            ResolveAnimatorIfMissing();
            ResolveAttackBehaviorIfMissing();
            DisableRootMotion();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (GuardTickInput(deltaTime) == false) return;
            if (joystick == null) { SetIdleIfNeeded(); return; }
            if (ShouldMove(joystick.Direction) == false) { StopMovementState(); return; }
            ApplyMovement(joystick.Direction, deltaTime);
        }

        public void SetJoystick(VirtualJoystickInput input)
        {
            if (GuardSetJoystickInput(input) == false) return;
            joystick = input;
        }

        private void ApplyMovement(Vector2 input, float deltaTime)
        {
            Vector2 mappedInput = MapInput(input);
            Vector3 movement = new Vector3(mappedInput.x, 0f, mappedInput.y);
            transform.position += movement * Mathf.Max(0f, movementSpeed) * deltaTime;
            RotateTowards(movement, deltaTime);
            SetRunIfNeeded();
            wasMoving = true;
        }

        private void SetRunIfNeeded()
        {
            if (CanSetRunState() == false) return;
            PlayState(runStateName, runFallbackStateName);
        }

        private void SetIdleIfNeeded()
        {
            if (CanSetIdleState() == false) return;
            PlayState(idleStateName, idleFallbackStateName);
        }

        private void ResolveJoystickIfMissing()
        {
            if (joystick != null) return;
            joystick = FindObjectOfType<VirtualJoystickInput>();
        }

        private void ResolveAnimatorIfMissing()
        {
            if (animator != null) return;
            animator = GetComponentInChildren<Animator>();
        }

        private void ResolveAttackBehaviorIfMissing()
        {
            if (attackBehavior != null) return;
            attackBehavior = GetComponent<PlayerAttackAnimationBehavior>();
        }

        private void DisableRootMotion()
        {
            if (animator == null) return;
            animator.applyRootMotion = false;
        }

        private bool GuardTickInput(float deltaTime)
        {
            return deltaTime >= 0f;
        }

        private bool GuardSetJoystickInput(VirtualJoystickInput input)
        {
            return input != null;
        }

        private bool ShouldMove(Vector2 input)
        {
            float threshold = inputThreshold * inputThreshold;
            return input.sqrMagnitude >= threshold;
        }

        private Vector2 MapInput(Vector2 input)
        {
            float x = ResolveHorizontal(input.x);
            float y = ResolveVertical(input.y);
            return new Vector2(x, y);
        }

        private void StopMovementState()
        {
            SetIdleIfNeeded();
            wasMoving = false;
        }

        private void RotateTowards(Vector3 movement, float deltaTime)
        {
            if (CanRotate(movement) == false) return;
            Quaternion target = Quaternion.LookRotation(movement.normalized, Vector3.up);
            float speed = Mathf.Max(0f, rotateLerpSpeed) * deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, target, speed);
        }

        private bool CanRotate(Vector3 movement)
        {
            if (rotateToDirection == false) return false;
            return movement.sqrMagnitude > 0f;
        }

        private bool CanSetRunState()
        {
            if (animator == null) return false;
            if (ShouldHoldAttackAnimation()) return false;
            return CanPlayState(runStateName, runFallbackStateName);
        }

        private bool CanSetIdleState()
        {
            if (animator == null) return false;
            if (ShouldHoldAttackAnimation()) return false;
            if (wasMoving == false) return false;
            return CanPlayState(idleStateName, idleFallbackStateName);
        }

        private bool ShouldHoldAttackAnimation()
        {
            if (attackBehavior == null) return false;
            return attackBehavior.IsAttackLocked;
        }

        private float ResolveHorizontal(float x)
        {
            if (invertHorizontalAxis == false) return x;
            return -x;
        }

        private float ResolveVertical(float y)
        {
            if (invertVerticalAxis == false) return y;
            return -y;
        }

        private bool CanPlayState(string stateName, string fallbackStateName)
        {
            if (HasState(stateName)) return true;
            return HasState(fallbackStateName);
        }

        private bool HasState(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName)) return false;
            int hash = Animator.StringToHash(stateName);
            return animator.HasState(0, hash);
        }

        private void PlayState(string stateName, string fallbackStateName)
        {
            string resolved = ResolveStateName(stateName, fallbackStateName);
            if (string.IsNullOrWhiteSpace(resolved)) return;
            animator.Play(resolved, 0, 0f);
        }

        private string ResolveStateName(string stateName, string fallbackStateName)
        {
            if (HasState(stateName)) return stateName;
            if (HasState(fallbackStateName)) return fallbackStateName;
            return string.Empty;
        }
    }
}
