using Madbox.Entities;
using UnityEngine;
using System;
using System.Collections.Generic;
using Madbox.App.GameView.Player;

namespace Madbox.Player
{
    /// <summary>
    /// Player entity data: <see cref="IsAlive"/> and <see cref="CanMove"/> use dedicated attributes and must have matching entries in the inherited list.
    /// </summary>
    public sealed class Player : Entity
    {
        public event Action<Weapon, Weapon> EquippedWeaponChanged;

        [SerializeField]
        private PlayerAttribute isAliveAttribute;

        [SerializeField]
        private PlayerAttribute canMoveAttribute;

        private readonly List<Weapon> availableWeapons = new List<Weapon>();
        private Weapon equippedWeapon;

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
                Weapon weapon = weaponInstances[i] != null
                    ? weaponInstances[i].GetComponentInChildren<Weapon>(true)
                    : null;
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

            IReadOnlyList<EntityAttributeModifierEntry> modifiers = weapon.Modifiers;
            for (int i = 0; i < modifiers.Count; i++)
            {
                EntityAttributeModifierEntry modifier = modifiers[i];
                if (modifier == null || modifier.Attribute == null)
                {
                    continue;
                }

                AddAttributeModifier(modifier.Attribute, modifier.Delta);
            }
        }

        public void Unequip(Weapon weapon)
        {
            if (weapon == null)
            {
                return;
            }

            IReadOnlyList<EntityAttributeModifierEntry> modifiers = weapon.Modifiers;
            for (int i = 0; i < modifiers.Count; i++)
            {
                EntityAttributeModifierEntry modifier = modifiers[i];
                if (modifier == null || modifier.Attribute == null)
                {
                    continue;
                }

                RemoveAttributeModifier(modifier.Attribute, modifier.Delta);
            }
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
