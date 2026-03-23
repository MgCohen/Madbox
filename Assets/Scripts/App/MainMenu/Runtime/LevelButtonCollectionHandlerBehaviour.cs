using Madbox.Levels;
using Scaffold.MVVM.Binding;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Madbox.App.MainMenu
{
    /// <summary>
    /// Instantiates a level button prefab under a container for each <see cref="AvailableLevel"/> via <see cref="ICollectionHandler{TSource,TTarget}"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelButtonCollectionHandlerBehaviour : MonoBehaviour, ICollectionHandler<AvailableLevel, MainMenuLevelListItem>
    {
        [SerializeField] private MainMenuLevelListItem levelButtonPrefab;
        [SerializeField] private Transform levelListContainer;

        public MainMenuLevelListItem Add(AvailableLevel source)
        {
            MainMenuLevelListItem instance = Instantiate(levelButtonPrefab, levelListContainer);
            instance?.SetLabel(source);
            return instance;
        }

        public void Remove(MainMenuLevelListItem item)
        {
            if (item == null)
            {
                return;
            }
            Destroy(item.gameObject);
        }

        private void WireLevelClick(Button button, AvailableLevel source)
        {
            AvailableLevel captured = source;
            button.onClick.AddListener(() => HandleLevelClicked(captured));
        }

        internal void HandleLevelClicked(AvailableLevel entry)
        {
            if (entry?.Definition == null)
            {
                return;
            }

            Debug.Log(entry.Definition.name);
        }
    }
}
