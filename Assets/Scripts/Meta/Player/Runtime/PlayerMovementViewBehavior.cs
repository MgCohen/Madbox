using Madbox.App.Animation;
using Madbox.Entities;
using UnityEngine;

namespace Madbox.Players
{
    /// <summary>
    /// Moves the player root from resolved move input; sets the animator locomotion bool so transitions can drive idle/run.
    /// When this component is on a child object (e.g. a "Behaviours" node), movement and facing apply to the parent <see cref="Player"/> transform unless <see cref="movementRoot"/> is assigned.
    /// </summary>
    public sealed class PlayerMovementViewBehavior : MonoBehaviour, IPlayerBehavior
    {
        [SerializeField]
        [Min(0f)]
        private float inputAcceptSqrThreshold = 0.025f;

        [SerializeField]
        private AnimationController animationController;

        [SerializeField]
        private EntityAttribute moveSpeedAttribute;

        [SerializeField]
        private EntityAttribute canMoveAttribute;

        [SerializeField]
        private EntityAttribute isAliveAttribute;

        [SerializeField]
        private AnimationAttribute movingParameter;

        [SerializeField]
        private bool invertMovementInput;

        [SerializeField]
        private bool invertFacingDirection;

        [SerializeField]
        private float rotateLerpSpeed = 12f;

        [SerializeField]
        [Tooltip("When set, this transform is moved and rotated. When null, uses the Player component on a parent (typically the hero root). Use this if behaviours live under a child object.")]
        private Transform movementRoot;

        private Transform movementTarget;

        private void Awake()
        {
            if (movementRoot != null)
            {
                movementTarget = movementRoot;
                return;
            }

            Player player = GetComponentInParent<Player>();
            movementTarget = player != null ? player.transform : transform;
        }

        public bool TryAcceptControl(Madbox.Players.Player data, in PlayerInputContext input)
        {
            if (data == null || !IsEnabled(data, canMoveAttribute) || !IsEnabled(data, isAliveAttribute))
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

        private static bool IsEnabled(Madbox.Players.Player data, EntityAttribute attribute)
        {
            if (attribute == null)
            {
                return true;
            }

            return data.GetBoolAttribute(attribute);
        }

        public void Execute(Madbox.Players.Player data, in PlayerInputContext input, float deltaTime)
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

        private void Move(Madbox.Players.Player data, float deltaTime, Vector3 world)
        {
            float speed = data.GetFloatAttribute(moveSpeedAttribute);
            movementTarget.position += world * (speed * deltaTime);
        }

        private void Rotate(float deltaTime, Vector3 world)
        {
            Vector3 dir = world.normalized;
            if (invertFacingDirection)
            {
                dir = -dir;
            }

            Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
            movementTarget.rotation = Quaternion.Slerp(movementTarget.rotation, target, 1f - Mathf.Exp(-rotateLerpSpeed * deltaTime));
        }

        public void OnQuit(Madbox.Players.Player data)
        {
            if (animationController != null && movingParameter != null)
            {
                animationController.SetBool(movingParameter, false);
            }
        }
    }
}
