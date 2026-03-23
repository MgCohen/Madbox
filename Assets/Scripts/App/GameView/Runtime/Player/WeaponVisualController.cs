using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madbox.App.GameView.Player
{
    public sealed class WeaponVisualController : MonoBehaviour
    {
        public event Action<int, int> SelectedWeaponChanged;

        public int SelectedWeaponIndex => selectedWeaponIndex;

        [SerializeField]
        private List<Transform> weaponSockets = new List<Transform>();

        private GameObject[] weaponInstances = Array.Empty<GameObject>();

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

            int previousIndex = selectedWeaponIndex;
            selectedWeaponIndex = index;
            ApplyWeaponActiveFlags(index);
            SelectedWeaponChanged?.Invoke(previousIndex, selectedWeaponIndex);
        }

        private void ApplyWeaponActiveFlags(int activeIndex)
        {
            for (int i = 0; i < weaponInstances.Length; i++)
            {
                weaponInstances[i].SetActive(i == activeIndex);
            }
        }

    }
}
