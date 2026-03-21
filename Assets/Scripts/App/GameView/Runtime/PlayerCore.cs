using UnityEngine;

namespace Madbox.App.GameView
{
    public sealed class PlayerCore : MonoBehaviour
    {
        public PlayerState State
        {
            get
            {
                if (viewData == null)
                {
                    return new PlayerState
                    {
                        IsAlive = true,
                        CanMove = true,
                        MoveSpeed = 3.5f
                    };
                }

                return new PlayerState
                {
                    IsAlive = viewData.IsAlive,
                    CanMove = viewData.CanMove,
                    MoveSpeed = viewData.MoveSpeed
                };
            }
        }

        [SerializeField] private PlayerViewData viewData;
    }
}
