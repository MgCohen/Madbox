using Scaffold.MVVM;
using UnityEngine;
using UnityEngine.UI;

namespace Madbox.App.Gameplay
{
    public sealed class GameView : UIView<GameViewModel>
    {
        [SerializeField] private Button backToMenuButton;

        protected override void OnBind()
        {
            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(OnBackClicked);
            }

            viewModel?.BeginSessionLoad(this);
        }

        protected override void OnUnbind()
        {
            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveListener(OnBackClicked);
            }
        }

        private void OnBackClicked()
        {
            viewModel?.ExitToMenu(this);
        }
    }
}
