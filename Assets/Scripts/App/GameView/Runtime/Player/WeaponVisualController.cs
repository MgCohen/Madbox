using System;
using System.Collections.Generic;
using Madbox.Entity;
using UnityEngine;

namespace Madbox.App.GameView.Player
{
    public sealed class WeaponVisualController : MonoBehaviour
    {
        public int SelectedWeaponIndex => selectedWeaponIndex;

        [SerializeField]
        private List<Transform> weaponSockets = new List<Transform>();

        [SerializeField]
        private EntityData modifierTarget;

        private GameObject[] weaponInstances = Array.Empty<GameObject>();
        private EntityAttributeModifierEntry[][] weaponModifiers = Array.Empty<EntityAttributeModifierEntry[]>();
        private readonly List<EntityAttributeModifierEntry> activeWeaponModifiers = new List<EntityAttributeModifierEntry>();

        private int selectedWeaponIndex = -1;

        public int WeaponSocketCount => weaponSockets != null ? weaponSockets.Count : 0;

        public Transform GetWeaponSocket(int index)
        {
            if (weaponSockets == null || index < 0 || index >= weaponSockets.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index is not within the weapon socket list.");
            }

            return weaponSockets[index];
        }

        public void SetModifierTarget(EntityData target)
        {
            modifierTarget = target;
            ReapplySelectedWeaponModifiers();
        }

        public void SetWeaponInstances(IReadOnlyList<GameObject> weapons)
        {
            if (weapons == null)
            {
                throw new ArgumentNullException(nameof(weapons));
            }

            int socketCount = WeaponSocketCount;
            if (weapons.Count != socketCount)
            {
                throw new InvalidOperationException(
                    $"Weapon instance count ({weapons.Count}) must match weapon socket count ({socketCount}).");
            }

            weaponInstances = new GameObject[weapons.Count];
            for (int i = 0; i < weapons.Count; i++)
            {
                weaponInstances[i] = weapons[i];
            }

            if (weaponInstances.Length > 0)
            {
                SetSelectedWeaponIndex(0);
            }
            else
            {
                selectedWeaponIndex = -1;
            }
        }

        public void SetWeaponModifiers(IReadOnlyList<IReadOnlyList<EntityAttributeModifierEntry>> modifiersPerWeapon)
        {
            if (modifiersPerWeapon == null)
            {
                throw new ArgumentNullException(nameof(modifiersPerWeapon));
            }

            weaponModifiers = new EntityAttributeModifierEntry[modifiersPerWeapon.Count][];
            for (int i = 0; i < modifiersPerWeapon.Count; i++)
            {
                IReadOnlyList<EntityAttributeModifierEntry> source = modifiersPerWeapon[i];
                if (source == null)
                {
                    weaponModifiers[i] = Array.Empty<EntityAttributeModifierEntry>();
                    continue;
                }

                var copied = new EntityAttributeModifierEntry[source.Count];
                for (int j = 0; j < source.Count; j++)
                {
                    copied[j] = source[j];
                }

                weaponModifiers[i] = copied;
            }

            if (weaponInstances.Length > 0 && weaponModifiers.Length != weaponInstances.Length)
            {
                throw new InvalidOperationException(
                    $"Weapon modifier count ({weaponModifiers.Length}) must match weapon instance count ({weaponInstances.Length}).");
            }

            ReapplySelectedWeaponModifiers();
        }

        public void SetSelectedWeaponIndex(int index)
        {
            if (weaponInstances.Length == 0)
            {
                throw new InvalidOperationException("Weapon instances are not set. Call SetWeaponInstances first.");
            }

            if (index < 0 || index >= weaponInstances.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index is not within the weapon instance list.");
            }

            RemoveActiveWeaponModifiers();
            selectedWeaponIndex = index;
            ApplyWeaponActiveFlags(index);
            ApplyWeaponModifiersFor(index);
        }

        private void ApplyWeaponActiveFlags(int activeIndex)
        {
            for (int i = 0; i < weaponInstances.Length; i++)
            {
                weaponInstances[i].SetActive(i == activeIndex);
            }
        }

        private void ReapplySelectedWeaponModifiers()
        {
            if (selectedWeaponIndex < 0)
            {
                return;
            }

            RemoveActiveWeaponModifiers();
            ApplyWeaponModifiersFor(selectedWeaponIndex);
        }

        private void ApplyWeaponModifiersFor(int index)
        {
            if (modifierTarget == null)
            {
                return;
            }

            if (weaponModifiers.Length == 0 || index < 0 || index >= weaponModifiers.Length)
            {
                return;
            }

            EntityAttributeModifierEntry[] selectedModifiers = weaponModifiers[index];
            for (int i = 0; i < selectedModifiers.Length; i++)
            {
                EntityAttributeModifierEntry modifier = selectedModifiers[i];
                if (modifier == null || modifier.Attribute == null)
                {
                    continue;
                }

                modifierTarget.AddAttributeModifier(modifier.Attribute, modifier.Delta);
                activeWeaponModifiers.Add(modifier);
            }
        }

        private void RemoveActiveWeaponModifiers()
        {
            if (modifierTarget == null || activeWeaponModifiers.Count == 0)
            {
                activeWeaponModifiers.Clear();
                return;
            }

            for (int i = 0; i < activeWeaponModifiers.Count; i++)
            {
                EntityAttributeModifierEntry modifier = activeWeaponModifiers[i];
                if (modifier == null || modifier.Attribute == null)
                {
                    continue;
                }

                modifierTarget.RemoveAttributeModifier(modifier.Attribute, modifier.Delta);
            }

            activeWeaponModifiers.Clear();
        }
    }
}
