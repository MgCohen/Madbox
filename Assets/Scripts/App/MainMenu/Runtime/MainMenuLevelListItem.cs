using Madbox.Levels;
using Scaffold.MVVM;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Madbox.App.MainMenu
{
    /// <summary>
    /// Optional root for level list entries: exposes the clickable <see cref="Button"/> and a label to set at bind time.
    /// </summary>
    public sealed class MainMenuLevelListItem : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI label;
        private AvailableLevel level;

        public void Set(AvailableLevel level)
        {
            this.level = level;
            label.text = $"{level.Definition.LevelId} - {level.AvailabilityState}";
            button.onClick.RemoveListener(Clicked);
            button.onClick.AddListener(Clicked);
        }

        private void Clicked()
        {
            var viewEvent = new LevelClickedViewEvent(level);
            ViewEvents.Raise(this, viewEvent);
        }
    }

    public class LevelClickedViewEvent : ViewEvent
    {
        public LevelClickedViewEvent(AvailableLevel level)
        {
            Level = level;
        }

        public AvailableLevel Level { get; private set; }
    }
}
