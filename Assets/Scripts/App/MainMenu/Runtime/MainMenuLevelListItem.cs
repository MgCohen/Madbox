using Madbox.Levels;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Madbox.App.MainMenu
{
    /// <summary>
    /// Optional root for level list entries: exposes the clickable <see cref="Button"/> and a label to set at bind time.
    /// </summary>
    public sealed class MainMenuLevelListItem : MonoBehaviour
    {
        public Button Button
        {
            get
            {
                if (button == null)
                {
                    button = GetComponent<Button>();
                }

                return button;
            }
        }

        [SerializeField] private Button button;
        [SerializeField] private Component label;

        public void Set(string text)
        {
            if (label == null)
            {
                return;
            }

            PropertyInfo property = label.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
            property?.SetValue(label, text);
        }

        internal void SetLabel(AvailableLevel source)
        {
            if (source == null)
            {
                return;
            }

            Set(source.MenuButtonLabel);
        }
    }
}
