using Madbox.Entities;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Madbox.Players
{
    public sealed class Player : Entity, IPlayerData
    {
        Transform IPlayerData.Transform => transform;

        private readonly List<Weapon> availableWeapons = new List<Weapon>();

        private Weapon equippedWeapon;

        /// <summary>
        /// Slice of <see cref="Entity.AttributeModifiers"/> added for the current <see cref="equippedWeapon"/>.
        /// Range removal avoids matching by attribute + delta (float equality and duplicate deltas).
        /// </summary>
        private int equippedModifierStartIndex = -1;

        private int equippedModifierCount;

        public event Action<Weapon, Weapon> EquippedWeaponChanged;

        public void SetAvailableWeapons(IReadOnlyList<GameObject> weaponInstances)
        {
            availableWeapons.Clear();
            if (weaponInstances == null)
            {
                SetEquippedWeapon(null);
                return;
            }

            for (int i = 0; i < weaponInstances.Count; i++)
            {
                Weapon weapon = weaponInstances[i] != null ? weaponInstances[i].GetComponentInChildren<Weapon>(true) : null;
                availableWeapons.Add(weapon);
            }

            if (availableWeapons.Count == 0)
            {
                SetEquippedWeapon(null);
                return;
            }

            SetEquippedWeapon(availableWeapons[0]);
        }

        public void EquipWeaponAtIndex(int index)
        {
            if (index < 0 || index >= availableWeapons.Count)
            {
                return;
            }

            SetEquippedWeapon(availableWeapons[index]);
        }

        public void Equip(Weapon weapon)
        {
            if (weapon == null)
            {
                return;
            }

            int startIndex = AttributeModifiers.Count;
            int added = 0;
            IReadOnlyList<EntityAttributeModifierEntry> modifiers = weapon.Modifiers;
            for (int i = 0; i < modifiers.Count; i++)
            {
                EntityAttributeModifierEntry modifier = modifiers[i];
                if (modifier == null || modifier.Attribute == null)
                {
                    continue;
                }

                AddAttributeModifier(modifier.Attribute, modifier.Delta);
                added++;
            }

            equippedModifierStartIndex = startIndex;
            equippedModifierCount = added;
        }

        public void Unequip(Weapon weapon)
        {
            if (weapon == null)
            {
                return;
            }

            if (equippedModifierStartIndex < 0)
            {
                return;
            }

            if (equippedModifierCount > 0)
            {
                RemoveAttributeModifiersAt(equippedModifierStartIndex, equippedModifierCount);
            }

            equippedModifierStartIndex = -1;
            equippedModifierCount = 0;
        }

        private void OnDisable()
        {
            SetEquippedWeapon(null);
        }

        private void SetEquippedWeapon(Weapon nextWeapon)
        {
            if (ReferenceEquals(equippedWeapon, nextWeapon))
            {
                return;
            }

            Weapon previousWeapon = equippedWeapon;
            if (previousWeapon != null)
            {
                Unequip(previousWeapon);
            }

            equippedWeapon = nextWeapon;
            if (equippedWeapon != null)
            {
                Equip(equippedWeapon);
            }

            EquippedWeaponChanged?.Invoke(previousWeapon, equippedWeapon);
        }
    }
}
