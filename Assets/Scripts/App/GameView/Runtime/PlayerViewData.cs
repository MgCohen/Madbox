using UnityEngine;

namespace Madbox.App.GameView
{
    public sealed class PlayerViewData : MonoBehaviour
    {
        public bool IsAlive => isAlive;
        [SerializeField] private bool isAlive = true;
        public bool CanMove => canMove;
        [SerializeField] private bool canMove = true;
        public float MoveSpeed => moveSpeed;
        [SerializeField] private float moveSpeed = 3.5f;
    }
}
