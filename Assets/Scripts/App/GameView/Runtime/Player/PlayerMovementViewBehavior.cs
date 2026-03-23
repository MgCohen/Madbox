using Madbox.App.Animation;
using UnityEngine;

namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Moves the player root from resolved move input; sets the animator locomotion bool so transitions can drive idle/run.
    /// </summary>
    public sealed class PlayerMovementViewBehavior : MonoBehaviour, IPlayerBehavior
    {
        [SerializeField]
        [Min(0f)]
        private float inputAcceptSqrThreshold = 0.025f;

        [SerializeField]
        private AnimationController animationController;

        [SerializeField]
        private PlayerAttribute moveSpeedAttribute;

        [SerializeField]
        private AnimationAttribute movingParameter;

        [SerializeField]
        private bool invertMovementInput;

        [SerializeField]
        private bool invertFacingDirection;

        [SerializeField]
        private float rotateLerpSpeed = 12f;

        public bool TryAcceptControl(Player data, in PlayerInputContext input)
        {
            if (data == null || !data.CanMove || !data.IsAlive)
            {
                return false;
            }

            if(input.MoveDirection.sqrMagnitude < inputAcceptSqrThreshold)
            {
                return false;
            }

            animationController.SetBool(movingParameter, true);
            return true;
        }

        public void Execute(Player data, in PlayerInputContext input, float deltaTime)
        {
            Vector2 move = input.MoveDirection;
            if (invertMovementInput)
            {
                move.x = -move.x;
                move.y = -move.y;
            }

            Vector3 world = new Vector3(move.x, 0f, move.y);
            Move(data, deltaTime, world);
            if (world.sqrMagnitude > 0.0001f)
            {
                Rotate(deltaTime, world);
            }
        }

        private void Move(Player data, float deltaTime, Vector3 world)
        {
            float speed = data.GetFloatAttribute(moveSpeedAttribute);
            transform.position += world * (speed * deltaTime);
        }

        private void Rotate(float deltaTime, Vector3 world)
        {
            Vector3 dir = world.normalized;
            if (invertFacingDirection)
            {
                dir = -dir;
            }

            Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-rotateLerpSpeed * deltaTime));
        }

        public void OnQuit(Player data)
        {
            if (animationController != null && movingParameter != null)
            {
                animationController.SetBool(movingParameter, false);
            }
        }
    }
}
