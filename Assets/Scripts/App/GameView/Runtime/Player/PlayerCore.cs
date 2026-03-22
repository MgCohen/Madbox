using UnityEngine;

namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Thin holder for <see cref="PlayerViewData"/> used by behaviors.
    /// </summary>
    public sealed class PlayerCore : MonoBehaviour
    {
        [SerializeField]
        private PlayerViewData viewData;

        public PlayerViewData ViewData => viewData;
    }
}
