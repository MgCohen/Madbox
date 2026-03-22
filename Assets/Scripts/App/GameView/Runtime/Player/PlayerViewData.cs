using UnityEngine;

namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Serialized view-side stats for the player character.
    /// </summary>
    public sealed class PlayerViewData : MonoBehaviour
    {
        [SerializeField]
        private bool isAlive = true;

        [SerializeField]
        private bool canMove = true;

        [SerializeField]
        private float moveSpeed = 3.5f;

        [SerializeField]
        [Min(0.05f)]
        private float attackSpeedStat = 1f;

        public bool IsAlive
        {
            get => isAlive;
            set => isAlive = value;
        }

        public bool CanMove
        {
            get => canMove;
            set => canMove = value;
        }

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }

        /// <summary>
        /// Gameplay attack speed; 1 is baseline. Drives attack animation speed multiplier only.
        /// </summary>
        public float AttackSpeedStat
        {
            get => attackSpeedStat;
            set => attackSpeedStat = Mathf.Max(0.05f, value);
        }
    }
}
