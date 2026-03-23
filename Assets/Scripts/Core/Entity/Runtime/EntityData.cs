using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madbox.Entities
{
    /// <summary>
    /// Float-backed attributes as <see cref="EntityAttribute"/> keys with storage in <see cref="attributeEntries"/>.
    /// Effective values are base + sum of <see cref="attributeModifiers"/>; recomputed when modifiers or bases change.
    /// Subclasses add typed accessors and game-specific rules.
    /// </summary>
    public class EntityData : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<EntityAttributeEntry> attributeEntries = new List<EntityAttributeEntry>();

        [SerializeField]
        private List<EntityAttributeModifierEntry> attributeModifiers = new List<EntityAttributeModifierEntry>();

        [NonSerialized]
        private bool modifiersDirty = true;

        /// <summary>
        /// Raised after the effective value changes (base change, modifier add/remove, or load-time recompute).
        /// </summary>
        public event Action<EntityAttribute, float> AttributeValueChanged;

        private void Awake()
        {
            modifiersDirty = true;
            RecalculateAttributesIfDirty();
        }

        private void OnEnable()
        {
            modifiersDirty = true;
            RecalculateAttributesIfDirty();
        }

        /// <summary>
        /// Read-only view of modifier entries (attribute reference + additive delta).
        /// </summary>
        public IReadOnlyList<EntityAttributeModifierEntry> AttributeModifiers => attributeModifiers;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            modifiersDirty = true;
        }

        /// <summary>
        /// Adds a modifier and recomputes effective attribute values.
        /// </summary>
        public void AddAttributeModifier(EntityAttribute attribute, float delta)
        {
            if (attribute == null)
            {
                return;
            }

            attributeModifiers.Add(new EntityAttributeModifierEntry(attribute, delta));
            RecalculateAttributes(force: true);
        }

        /// <summary>
        /// Removes the first modifier that matches both attribute and delta; then recomputes values.
        /// </summary>
        /// <returns>True if a matching modifier was removed.</returns>
        public bool RemoveAttributeModifier(EntityAttribute attribute, float delta)
        {
            if (attribute == null)
            {
                return false;
            }

            for (int i = 0; i < attributeModifiers.Count; i++)
            {
                EntityAttributeModifierEntry m = attributeModifiers[i];
                if (m.Attribute != attribute)
                {
                    continue;
                }

                if (!Mathf.Approximately(m.Delta, delta))
                {
                    continue;
                }

                attributeModifiers.RemoveAt(i);
                RecalculateAttributes(force: true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears all modifiers and recomputes effective values.
        /// </summary>
        public void ClearAttributeModifiers()
        {
            if (attributeModifiers.Count == 0)
            {
                return;
            }

            attributeModifiers.Clear();
            RecalculateAttributes(force: true);
        }

        public float GetFloatAttribute(EntityAttribute attribute)
        {
            if (attribute == null)
            {
                return 0f;
            }

            RecalculateAttributesIfDirty();

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

        public void SetFloatAttribute(EntityAttribute attribute, float newBaseValue)
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

                if (!attributeEntries[i].SetBaseValue(newBaseValue))
                {
                    return;
                }

                modifiersDirty = true;
                RecalculateAttributes(force: true);
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

        private void RecalculateAttributesIfDirty()
        {
            RecalculateAttributes(force: false);
        }

        private void RecalculateAttributes(bool force)
        {
            if (!force && !modifiersDirty)
            {
                return;
            }

            modifiersDirty = false;

            var sums = new Dictionary<EntityAttribute, float>();

            for (int m = 0; m < attributeModifiers.Count; m++)
            {
                EntityAttributeModifierEntry mod = attributeModifiers[m];
                EntityAttribute key = mod.Attribute;
                if (key == null)
                {
                    continue;
                }

                if (sums.TryGetValue(key, out float existing))
                {
                    sums[key] = existing + mod.Delta;
                }
                else
                {
                    sums[key] = mod.Delta;
                }
            }

            for (int i = 0; i < attributeEntries.Count; i++)
            {
                EntityAttributeEntry entry = attributeEntries[i];
                EntityAttribute attr = entry.Attribute;
                if (attr == null)
                {
                    continue;
                }

                float modifierTotal = sums.TryGetValue(attr, out float s) ? s : 0f;
                float effective = entry.BaseValue + modifierTotal;
                if (entry.TrySetEffectiveValue(effective))
                {
                    AttributeValueChanged?.Invoke(attr, effective);
                }
            }
        }
    }
}
