using UnityEngine;

namespace Madbox.App.GameView
{
    public sealed class PlayerMovementViewBehavior : MonoBehaviour
    {
        [SerializeField] private VirtualJoystickInput joystick;
        [SerializeField] private PlayerAttackAnimationBehavior animationController;
        [SerializeField] private float movementSpeed = 3.5f;
        [SerializeField] private float inputThreshold = 0.05f;
        [SerializeField] private bool invertHorizontalAxis = true;
        [SerializeField] private bool invertVerticalAxis = true;
        [SerializeField] private bool rotateToDirection = true;
        [SerializeField] private bool invertFacingDirection = true;
        [SerializeField] private float rotateLerpSpeed = 12f;

        private void Awake()
        {
            ResolveJoystickIfMissing();
            ResolveAnimationControllerIfMissing();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (GuardTickInput(deltaTime) == false) return;
            if (joystick == null) { SetIdleAnimation(); return; }
            if (ShouldMove(joystick.Direction) == false) { SetIdleAnimation(); return; }
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
            SetRunAnimation();
        }

        private void ResolveJoystickIfMissing()
        {
            if (joystick != null) return;
            joystick = FindObjectOfType<VirtualJoystickInput>();
        }

        private void ResolveAnimationControllerIfMissing()
        {
            if (animationController != null) return;
            animationController = GetComponent<PlayerAttackAnimationBehavior>();
            if (animationController != null) return;
            animationController = GetComponentInChildren<PlayerAttackAnimationBehavior>();
            if (animationController != null) return;
            animationController = gameObject.AddComponent<PlayerAttackAnimationBehavior>();
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

        private void RotateTowards(Vector3 movement, float deltaTime)
        {
            if (CanRotate(movement) == false) return;
            Vector3 facing = ResolveFacingDirection(movement);
            Quaternion target = Quaternion.LookRotation(facing, Vector3.up);
            float speed = Mathf.Max(0f, rotateLerpSpeed) * deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, target, speed);
        }

        private Vector3 ResolveFacingDirection(Vector3 movement)
        {
            if (invertFacingDirection == false) return movement.normalized;
            return -movement.normalized;
        }

        private bool CanRotate(Vector3 movement)
        {
            if (rotateToDirection == false) return false;
            return movement.sqrMagnitude > 0f;
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

        private void SetRunAnimation()
        {
            if (animationController == null) return;
            animationController.SetMoving(true);
        }

        private void SetIdleAnimation()
        {
            if (animationController == null) return;
            animationController.SetMoving(false);
        }
    }
}
