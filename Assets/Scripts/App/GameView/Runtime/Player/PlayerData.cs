using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Player stats and flags as <see cref="PlayerAttribute"/> keys with float storage in <see cref="attributeEntries"/>.
    /// <see cref="IsAlive"/> and <see cref="CanMove"/> use dedicated attributes and must have matching entries.
    /// </summary>
    public sealed class PlayerData : MonoBehaviour
    {
        [SerializeField]
        private PlayerAttribute isAliveAttribute;

        [SerializeField]
        private PlayerAttribute canMoveAttribute;

        [SerializeField]
        private List<PlayerAttributeEntry> attributeEntries = new List<PlayerAttributeEntry>();

        /// <summary>
        /// Raised after a successful <see cref="SetFloatAttribute"/> that changed a value.
        /// </summary>
        public event Action<PlayerAttribute, float> AttributeValueChanged;

        public bool IsAlive
        {
            get => GetBoolAttribute(isAliveAttribute);
            set => SetBoolAttribute(isAliveAttribute, value);
        }

        public bool CanMove
        {
            get => GetBoolAttribute(canMoveAttribute);
            set => SetBoolAttribute(canMoveAttribute, value);
        }

        public float GetFloatAttribute(PlayerAttribute attribute)
        {
            if (attribute == null)
            {
                return 0f;
            }

            for (int i = 0; i < attributeEntries.Count; i++)
            {
                if (attributeEntries[i].Attribute == attribute)
                {
                    return attributeEntries[i].Value;
                }
            }

            return 0f;
        }

        public bool GetBoolAttribute(PlayerAttribute attribute)
        {
            return GetFloatAttribute(attribute) > 0f;
        }

        public void SetFloatAttribute(PlayerAttribute attribute, float newValue)
        {
            if (attribute == null)
            {
                return;
            }

            for (int i = 0; i < attributeEntries.Count; i++)
            {
                if (attributeEntries[i].Attribute != attribute)
                {
                    continue;
                }

                if (!attributeEntries[i].SetValue(newValue))
                {
                    return;
                }

                AttributeValueChanged?.Invoke(attribute, attributeEntries[i].Value);
                return;
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogWarning($"{nameof(PlayerData)}: no entry for attribute '{attribute.name}'.", this);
#endif
        }

        public void SetBoolAttribute(PlayerAttribute attribute, bool newValue)
        {
            SetFloatAttribute(attribute, newValue ? 1f : 0f);
        }
    }
}
