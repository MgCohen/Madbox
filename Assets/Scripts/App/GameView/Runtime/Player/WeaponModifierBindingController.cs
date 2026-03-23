using System.Collections.Generic;
using Madbox.Entity;
using UnityEngine;

namespace Madbox.App.GameView.Player
{
    public sealed class WeaponModifierBindingController : MonoBehaviour
    {
        [SerializeField]
        private PlayerData playerData;

        [SerializeField]
        private WeaponVisualController weaponVisualController;

        private readonly List<EntityAttributeModifierEntry> activeModifiers = new List<EntityAttributeModifierEntry>();

        public void Bind(WeaponVisualController visual, PlayerData data, IReadOnlyList<GameObject> weaponInstances)
        {
            if (weaponVisualController != null)
            {
                weaponVisualController.SelectedWeaponChanged -= OnSelectedWeaponChanged;
            }

            RemoveActiveModifiers();
            weaponVisualController = visual;
            playerData = data;

            if (weaponVisualController == null || playerData == null)
            {
                return;
            }

            if (isActiveAndEnabled)
            {
                weaponVisualController.SelectedWeaponChanged += OnSelectedWeaponChanged;
                ApplyWeaponModifiersAtIndex(weaponVisualController.SelectedWeaponIndex);
            }
        }

        private void OnEnable()
        {
            if (weaponVisualController == null || playerData == null)
            {
                return;
            }

            weaponVisualController.SelectedWeaponChanged += OnSelectedWeaponChanged;
            ApplyWeaponModifiersAtIndex(weaponVisualController.SelectedWeaponIndex);
        }

        private void OnDisable()
        {
            if (weaponVisualController != null)
            {
                weaponVisualController.SelectedWeaponChanged -= OnSelectedWeaponChanged;
            }

            RemoveActiveModifiers();
        }

        private void OnSelectedWeaponChanged(int previousIndex, int nextIndex)
        {
            RemoveActiveModifiers();
            ApplyWeaponModifiersAtIndex(nextIndex);
        }

        private void ApplyWeaponModifiersAtIndex(int index)
        {
            if (weaponVisualController == null || playerData == null)
            {
                return;
            }

            if (index < 0 || index >= weaponVisualController.WeaponSocketCount)
            {
                return;
            }

            Transform socket = weaponVisualController.GetWeaponSocket(index);
            Weapon weapon = socket.GetComponentInChildren<Weapon>(true);
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

                playerData.AddAttributeModifier(modifier.Attribute, modifier.Delta);
                activeModifiers.Add(modifier);
            }
        }

        private void RemoveActiveModifiers()
        {
            if (playerData == null || activeModifiers.Count == 0)
            {
                activeModifiers.Clear();
                return;
            }

            for (int i = 0; i < activeModifiers.Count; i++)
            {
                EntityAttributeModifierEntry modifier = activeModifiers[i];
                if (modifier == null || modifier.Attribute == null)
                {
                    continue;
                }

                playerData.RemoveAttributeModifier(modifier.Attribute, modifier.Delta);
            }

            activeModifiers.Clear();
        }
    }
}
