using System.Reflection;
using Madbox.Levels;
using Scaffold.MVVM;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Madbox.App.MainMenu
{
    public class MainMenuView : UIView<MainMenuViewModel>
    {
        [SerializeField] private TextMeshProUGUI goldLabel;
        [SerializeField] private Button addGoldButton;
        [SerializeField] private LevelButtonCollectionHandlerBehaviour levelButtonCollectionHandler;

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

        private void BindAddGoldButton()
        {
            addGoldButton.onClick.AddListener(OnAddGoldClicked);
        }

        private void UpdateGoldText(int value)
        {
            goldLabel.text = $"Gold: {value}";
        }


        protected override void OnUnbind()
        {
            if (addGoldButton != null)
            {
                addGoldButton.onClick.RemoveListener(OnAddGoldClicked);
            }
        }

        public void OnAddGoldClicked()
        {
            viewModel?.AddOneGold();
        }
    }
}
