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
    public class Entity : MonoBehaviour
    {
        public IReadOnlyList<EntityAttributeModifierEntry> AttributeModifiers => attributeModifiers;
        [SerializeField] private List<EntityAttributeModifierEntry> attributeModifiers = new List<EntityAttributeModifierEntry>();

        [SerializeField]
        private List<EntityAttributeEntry> attributeEntries = new List<EntityAttributeEntry>();
   
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

        public void AddAttributeModifier(EntityAttribute attribute, float delta)
        {
            if (attribute == null)
            {
                return;
            }

            attributeModifiers.Add(new EntityAttributeModifierEntry(attribute, delta));
            RecalculateAttributes(force: true);
        }

        public bool RemoveAttributeModifier(EntityAttribute attribute, float delta)
        {
            if (attribute == null)
            {
                return false;
            }

            for (int i = 0; i < attributeModifiers.Count; i++)
            {
                EntityAttributeModifierEntry m = attributeModifiers[i];
                if (m == null)
                {
                    continue;
                }

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
        /// Removes a contiguous slice of <see cref="attributeModifiers"/> by index (e.g. a batch added after <see cref="AddAttributeModifier"/>).
        /// Prefer this over repeated <see cref="RemoveAttributeModifier"/> when undoing a known equip batch.
        /// </summary>
        public void RemoveAttributeModifiersAt(int startIndex, int count)
        {
            if (count <= 0)
            {
                return;
            }

            if (startIndex < 0 || startIndex > attributeModifiers.Count - count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startIndex),
                    $"Invalid modifier range: startIndex={startIndex}, count={count}, listCount={attributeModifiers.Count}.");
            }

            attributeModifiers.RemoveRange(startIndex, count);
            RecalculateAttributes(force: true);
        }

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
                EntityAttributeEntry entry = attributeEntries[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry.Attribute == attribute)
                {
                    return entry.Value;
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
                EntityAttributeEntry entry = attributeEntries[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry.Attribute != attribute)
                {
                    continue;
                }

                if (!entry.SetBaseValue(newBaseValue))
                {
                    return;
                }

                modifiersDirty = true;
                RecalculateAttributes(force: true);
                return;
            }
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
                if (mod == null)
                {
                    continue;
                }

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
                if (entry == null)
                {
                    continue;
                }

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
