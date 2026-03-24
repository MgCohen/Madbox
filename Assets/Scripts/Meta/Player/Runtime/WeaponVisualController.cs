using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madbox.Players
{
    public sealed class WeaponVisualController : MonoBehaviour
    {
        [SerializeField]
        private Transform weaponSocket;

        private IReadOnlyList<GameObject> weaponInstances = Array.Empty<GameObject>();

        private int selectedWeaponIndex = -1;

        public int SelectedWeaponIndex => selectedWeaponIndex;

        public Transform WeaponSocket => weaponSocket;

        public event Action<int, int> SelectedWeaponChanged;

        public int IndexOfWeaponInstance(GameObject weaponInstance)
        {
            if (weaponInstance == null)
            {
                return -1;
            }

            for (int i = 0; i < weaponInstances.Count; i++)
            {
                if (weaponInstances[i] == weaponInstance)
                {
                    return i;
                }
            }

            return -1;
        }

        public void SetWeaponInstances(IReadOnlyList<GameObject> weapons)
        {
            if (weapons == null)
            {
                throw new ArgumentNullException(nameof(weapons));
            }

            weaponInstances = weapons;
            selectedWeaponIndex = -1;
            if (weaponInstances.Count > 0)
            {
                SetSelectedWeaponIndex(0);
            }
        }

        public void SetSelectedWeaponIndex(int index)
        {
            if (weaponInstances.Count == 0)
            {
                throw new InvalidOperationException("Weapon instances are not set. Call SetWeaponInstances first.");
            }

            if (index < 0 || index >= weaponInstances.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index is not within the weapon instance list.");
            }

            int previousIndex = selectedWeaponIndex;
            selectedWeaponIndex = index;
            ApplyWeaponActiveFlags(index);
            SelectedWeaponChanged?.Invoke(previousIndex, selectedWeaponIndex);
        }

        private void ApplyWeaponActiveFlags(int activeIndex)
        {
            for (int i = 0; i < weaponInstances.Count; i++)
            {
                weaponInstances[i].SetActive(i == activeIndex);
            }
        }
    }
}
