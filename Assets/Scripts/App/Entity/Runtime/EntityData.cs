using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madbox.App.Entity
{
    /// <summary>
    /// Float-backed attributes as <see cref="EntityAttribute"/> keys with storage in <see cref="attributeEntries"/>.
    /// Subclasses add typed accessors and game-specific rules.
    /// </summary>
    public class EntityData : MonoBehaviour
    {
        [SerializeField]
        private List<EntityAttributeEntry> attributeEntries = new List<EntityAttributeEntry>();

        /// <summary>
        /// Raised after a successful <see cref="SetFloatAttribute"/> that changed a value.
        /// </summary>
        public event Action<EntityAttribute, float> AttributeValueChanged;

        public float GetFloatAttribute(EntityAttribute attribute)
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

        public bool GetBoolAttribute(EntityAttribute attribute)
        {
            return GetFloatAttribute(attribute) > 0f;
        }

        public void SetFloatAttribute(EntityAttribute attribute, float newValue)
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
            Debug.LogWarning($"{nameof(EntityData)}: no entry for attribute '{attribute.name}'.", this);
#endif
        }

        public void SetBoolAttribute(EntityAttribute attribute, bool newValue)
        {
            SetFloatAttribute(attribute, newValue ? 1f : 0f);
        }
    }
}
