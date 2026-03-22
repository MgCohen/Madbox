using System.Reflection;
using Madbox.Levels;
using Scaffold.MVVM;
using UnityEngine;
using UnityEngine.UI;

namespace Madbox.App.MainMenu
{
    public class MainMenuView : UIView<MainMenuViewModel>
    {
        [SerializeField] private Component goldLabel;
        [SerializeField] private Button addGoldButton;
        [SerializeField] private GameObject levelButtonPrefab;
        [SerializeField] private Transform levelListContainer;
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

            if (levelButtonPrefab != null && levelListContainer != null)
            {
                LevelButtonCollectionHandlerBehaviour handler = EnsureLevelButtonCollectionHandler();
                handler.Attach(this, levelButtonPrefab, levelListContainer);
                BindCollection<AvailableLevel, Button>(() => viewModel.AvailableLevels, handler);
            }
        }

        private LevelButtonCollectionHandlerBehaviour EnsureLevelButtonCollectionHandler()
        {
            if (levelButtonCollectionHandler != null)
            {
                return levelButtonCollectionHandler;
            }

            levelButtonCollectionHandler = GetComponent<LevelButtonCollectionHandlerBehaviour>();
            if (levelButtonCollectionHandler == null)
            {
                levelButtonCollectionHandler = gameObject.AddComponent<LevelButtonCollectionHandlerBehaviour>();
            }

            return levelButtonCollectionHandler;
        }

        private void BindAddGoldButton()
        {
            addGoldButton.onClick.AddListener(OnAddGoldClicked);
        }

        private void UpdateGoldText(int value)
        {
            TrySetTextProperty(goldLabel, $"Gold: {value}");
        }

        internal void HandleLevelClicked(AvailableLevel entry)
        {
            if (entry?.Definition == null)
            {
                return;
            }

            Debug.Log(entry.Definition.name);
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

        private static void TrySetTextProperty(Component target, string text)
        {
            if (target == null)
            {
                return;
            }

            PropertyInfo property = target.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
            property?.SetValue(target, text);
        }
    }
}
