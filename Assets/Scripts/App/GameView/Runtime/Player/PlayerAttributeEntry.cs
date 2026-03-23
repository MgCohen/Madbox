using System;
using UnityEngine;
using UnityEngine.Events;

namespace Madbox.App.GameView.Player
{

    /// <summary>
    /// One named attribute value on <see cref="PlayerData"/>; optional inspector callback when the value changes.
    /// </summary>
    [Serializable]
    public sealed class PlayerAttributeEntry
    {
        public PlayerAttribute Attribute => attribute;
        [SerializeField] private PlayerAttribute attribute;

        public float Value => value;
        [SerializeField] private float value;

        [SerializeField]
        private UnityEvent<float> onValueChanged;

        public bool SetValue(float newValue)
        {
            if (Mathf.Approximately(value, newValue))
            {
                return false;
            }

            value = newValue;
            onValueChanged?.Invoke(value);
            return true;
        }
    }
}
