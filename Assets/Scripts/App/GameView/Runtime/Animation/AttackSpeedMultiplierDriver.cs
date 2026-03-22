using Madbox.App.GameView.Player;
using UnityEngine;

namespace Madbox.App.GameView.Animation
{
    /// <summary>
    /// Sets <see cref="PlayerAnimationController.AttackSpeedMultiplierParameter"/> each frame (for enemies without <see cref="Player.PlayerViewData"/>).
    /// </summary>
    public sealed class AttackSpeedMultiplierDriver : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;

        [SerializeField]
        private float multiplier = 1f;

        private int parameterHash;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            parameterHash = Animator.StringToHash(PlayerAnimationController.AttackSpeedMultiplierParameter);
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(parameterHash, multiplier);
        }
    }
}
