using Madbox.Levels;
using Scaffold.MVVM;
using TMPro;
using UnityEngine;
using Scaffold.MVVM.Binding;
using UnityEngine.UI;
using DG.Tweening;

namespace Madbox.App.MainMenu
{
    public class MainMenuView : UIView<MainMenuViewModel>
    {
        [SerializeField] private TextMeshProUGUI goldLabel;
        [SerializeField] private Button addGoldButton;
        [SerializeField] private LevelButtonCollectionHandlerBehaviour levelButtonCollectionHandler;
        [SerializeField] private TextMeshProUGUI jokeLabel;

        protected override void OnBind()
        {
            if (goldLabel != null)
            {
                Bind<int, int>(() => viewModel.Wallet.CurrentGold, UpdateGoldText);
            }

            if (addGoldButton != null)
            {
                BindAddGoldButton();
            }

            if (levelButtonCollectionHandler != null)
            {
                levelButtonCollectionHandler.SetLevelSelectHandler(viewModel.PlayLevel);
                BindCollection(() => viewModel.AvailableLevels, levelButtonCollectionHandler);
            }
        }

        protected override void OnUnbind()
        {
            if (addGoldButton != null)
            {
                addGoldButton.onClick.RemoveListener(OnAddGoldClicked);
            }
        }

        private void BindAddGoldButton()
        {
            addGoldButton.onClick.AddListener(OnAddGoldClicked);
        }

        private void UpdateGoldText(int value)
        {
            goldLabel.text = value.ToString();
        }

        public void OnAddGoldClicked()
        {
            viewModel?.AddOneGold();
            if (!jokeLabel.isActiveAndEnabled)
            {
                jokeLabel.gameObject.SetActive(true);
                jokeLabel.transform.DOScale(1, 0.4f).From(0).SetEase(Ease.OutBack);
            }
        }
    }
}
