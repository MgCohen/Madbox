using Madbox.Levels;
using Scaffold.MVVM.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace Madbox.App.MainMenu
{
    /// <summary>
    /// Instantiates a level button prefab under a container for each <see cref="AvailableLevel"/> via <see cref="ICollectionHandler{TSource,TTarget}"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelButtonCollectionHandlerBehaviour : MonoBehaviour, ICollectionHandler<AvailableLevel, Button>
    {
        private MainMenuView owner;
        private GameObject levelButtonPrefab;
        private Transform levelListContainer;

        public void Attach(MainMenuView menuView, GameObject prefab, Transform container)
        {
            owner = menuView;
            levelButtonPrefab = prefab;
            levelListContainer = container;
        }

        public Button Add(AvailableLevel source)
        {
            ThrowIfNotConfigured();
            GameObject instance = Instantiate(levelButtonPrefab, levelListContainer);
            Button button = ResolveButton(instance, out MainMenuLevelListItem listItem);
            listItem?.SetLabel(source.MenuButtonLabel);
            WireLevelClick(button, source);
            return button;
        }

        public void Remove(Button item)
        {
            if (item == null)
            {
                return;
            }

            item.onClick.RemoveAllListeners();
            Destroy(item.gameObject);
        }

        private void ThrowIfNotConfigured()
        {
            if (owner == null || levelButtonPrefab == null || levelListContainer == null)
            {
                throw new System.InvalidOperationException(
                    "LevelButtonCollectionHandlerBehaviour requires MainMenuView, level button prefab, and level list container.");
            }
        }

        private Button ResolveButton(GameObject instance, out MainMenuLevelListItem listItem)
        {
            listItem = instance.GetComponent<MainMenuLevelListItem>();
            Button button = listItem != null ? listItem.Button : instance.GetComponentInChildren<Button>(true);
            if (button == null)
            {
                Destroy(instance);
                throw new System.InvalidOperationException(
                    "Level button prefab must include a Button or a " + nameof(MainMenuLevelListItem) + " with a button.");
            }

            return button;
        }

        private void WireLevelClick(Button button, AvailableLevel source)
        {
            AvailableLevel captured = source;
            button.onClick.AddListener(() => owner.HandleLevelClicked(captured));
        }
    }
}
