using Madbox.Player;
using UnityEngine;

namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Bridges gameplay weapon equip state and weapon visuals.
    /// Gameplay authority remains in <see cref="Player"/>; this controller updates view state.
    /// </summary>
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField]
        private Player playerData;

        [SerializeField]
        private WeaponVisualController weaponVisualController;

        public void Bind(Player data, WeaponVisualController visual)
        {
            if (playerData != null)
            {
                playerData.EquippedWeaponChanged -= OnEquippedWeaponChanged;
            }

            playerData = data;
            weaponVisualController = visual;

            if (playerData == null || weaponVisualController == null)
            {
                return;
            }

            if (isActiveAndEnabled)
            {
                playerData.EquippedWeaponChanged += OnEquippedWeaponChanged;
            }
        }

        private void OnEnable()
        {
            if (playerData == null || weaponVisualController == null)
            {
                return;
            }

            playerData.EquippedWeaponChanged += OnEquippedWeaponChanged;
        }

        private void OnDisable()
        {
            if (playerData != null)
            {
                playerData.EquippedWeaponChanged -= OnEquippedWeaponChanged;
            }
        }

        private void OnEquippedWeaponChanged(Weapon previousWeapon, Weapon currentWeapon)
        {
            if (weaponVisualController == null || currentWeapon == null)
            {
                return;
            }

            int index = FindWeaponIndex(currentWeapon.gameObject);
            if (index < 0)
            {
                return;
            }

            weaponVisualController.SetSelectedWeaponIndex(index);
        }

        private int FindWeaponIndex(GameObject weaponInstance)
        {
            int socketCount = weaponVisualController.WeaponSocketCount;
            for (int i = 0; i < socketCount; i++)
            {
                Transform socket = weaponVisualController.GetWeaponSocket(i);
                if (socket == null)
                {
                    continue;
                }

                if (weaponInstance.transform.IsChildOf(socket))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
