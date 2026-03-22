using System;
using UnityEngine;
using UnityEngine.Events;

namespace Madbox.App.Entity
{
    /// <summary>
    /// One named attribute value on <see cref="EntityData"/>; optional inspector callback when the value changes.
    /// </summary>
    [Serializable]
    public class EntityAttributeEntry
    {
        public EntityAttribute Attribute => attribute;

        [SerializeField]
        private EntityAttribute attribute;

        public float Value => value;

        [SerializeField]
        private float value;

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
