using Madbox.Levels;
using Scaffold.MVVM;
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

        private Action<AvailableLevel> onLevelSelected;

        private void OnEnable()
        {
            ViewEvents.Register<LevelClickedViewEvent>(this, HandleLevelClicked);
        }

        private void HandleLevelClicked(LevelClickedViewEvent @event)
        {
            onLevelSelected?.Invoke(@event.Level);
        }

        public void SetLevelSelectHandler(Action<AvailableLevel> handler)
        {
            onLevelSelected = handler;
        }

        public MainMenuLevelListItem Add(AvailableLevel source)
        {
            MainMenuLevelListItem instance = Instantiate(levelButtonPrefab, levelListContainer);
            if (instance != null)
            {
                instance.Set(source);
            }

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
    }
}
