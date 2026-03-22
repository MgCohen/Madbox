using Madbox.App.GameView.Input;
using UnityEngine;

namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Moves the player root from joystick or keyboard fallback; drives locomotion animation.
    /// </summary>
    public sealed class PlayerMovementViewBehavior : MonoBehaviour, IPlayerBehavior
    {
        [SerializeField]
        private VirtualJoystickInput joystick;

        [SerializeField]
        private PlayerInputProvider inputProviderOverride;

        [SerializeField]
        private PlayerAnimationController animationController;

        [SerializeField]
        private float movementSpeed = 3.5f;

        [SerializeField]
        private float inputThreshold = 0.05f;

        [SerializeField]
        private bool invertHorizontalAxis;

        [SerializeField]
        private bool invertVerticalAxis;

        [SerializeField]
        private bool rotateToDirection = true;

        [SerializeField]
        private bool invertFacingDirection;

        [SerializeField]
        private float rotateLerpSpeed = 12f;

        public bool TryAcceptControl(PlayerCore core)
        {
            if (core == null || core.ViewData == null || !core.ViewData.CanMove || !core.ViewData.IsAlive)
            {
                return false;
            }

            return ReadMoveInput().sqrMagnitude > inputThreshold * inputThreshold;
        }

        public void Execute(PlayerCore core, float deltaTime)
        {
            Vector2 input = ReadMoveInput();
            if (invertHorizontalAxis)
            {
                input.x = -input.x;
            }

            if (invertVerticalAxis)
            {
                input.y = -input.y;
            }

            Vector3 world = new Vector3(input.x, 0f, input.y);
            float speed = movementSpeed > 0f ? movementSpeed : core.ViewData.MoveSpeed;
            transform.position += world * (speed * deltaTime);

            if (animationController != null)
            {
                animationController.SetLocomotionMoving(true);
            }

            if (rotateToDirection && world.sqrMagnitude > 0.0001f)
            {
                Vector3 dir = world.normalized;
                if (invertFacingDirection)
                {
                    dir = -dir;
                }

                Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-rotateLerpSpeed * deltaTime));
            }
        }

        private Vector2 ReadMoveInput()
        {
            if (inputProviderOverride != null)
            {
                return inputProviderOverride.GetMoveDirection();
            }

            if (joystick != null && joystick.Direction.sqrMagnitude > 0.0001f)
            {
                return joystick.Direction;
            }

            float x = UnityEngine.Input.GetAxisRaw("Horizontal");
            float y = UnityEngine.Input.GetAxisRaw("Vertical");
            Vector2 kb = new Vector2(x, y);
            return kb.sqrMagnitude > 1f ? kb.normalized : kb;
        }
    }
}
