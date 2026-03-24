using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.Levels;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Players
{
    public sealed class PlayerFactory
    {
        public PlayerFactory(PlayerService playerService, IAddressablesGateway gateway)
        {
            this.playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
            this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        private readonly PlayerService playerService;

        private readonly IAddressablesGateway gateway;

        /// <summary>
        /// Loads player and weapon prefabs via Addressables, instantiates, and registers each load handle into
        /// <paramref name="sessionAddressableHandles"/> for the battle orchestrator to release when the match ends.
        /// </summary>
        public async Task<Player> CreateReadyPlayerAsync(
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            IList<IAssetHandle> sessionAddressableHandles,
            CancellationToken cancellationToken = default)
        {
            if (sessionAddressableHandles == null)
            {
                throw new ArgumentNullException(nameof(sessionAddressableHandles));
            }

            PlayerLoadoutDefinition loadout = playerService.Loadout;
            GameObject playerInstance = null;
            var acquiredHandles = new List<IAssetHandle>();
            try
            {
                AddressablePrefabSpawn playerSpawn = await InstantiatePlayerFromReferenceAsync(loadout.PlayerPrefab, parent, position, rotation, cancellationToken);
                playerInstance = playerSpawn.Instance;
                acquiredHandles.Add(playerSpawn.Handle);
                bool restoreActiveAfterSetup = playerInstance.activeSelf;
                if (restoreActiveAfterSetup)
                {
                    playerInstance.SetActive(false);
                }

                Player playerData = await AttachWeaponsAsync(loadout, playerInstance, acquiredHandles, cancellationToken);
                foreach (IAssetHandle handle in acquiredHandles)
                {
                    sessionAddressableHandles.Add(handle);
                }

                playerInstance.transform.SetPositionAndRotation(position, rotation);
                if (restoreActiveAfterSetup)
                {
                    playerInstance.SetActive(true);
                }

                return playerData;
            }
            catch
            {
                foreach (IAssetHandle handle in acquiredHandles)
                {
                    if (handle != null && !handle.IsReleased)
                    {
                        handle.Release();
                    }
                }

                if (playerInstance != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(playerInstance);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(playerInstance);
                    }
                }

                throw;
            }
        }

        private async Task<Player> AttachWeaponsAsync(
            PlayerLoadoutDefinition loadout,
            GameObject playerInstance,
            List<IAssetHandle> acquiredHandles,
            CancellationToken cancellationToken)
        {
            WeaponVisualController visual = playerInstance.GetComponentInChildren<WeaponVisualController>(true);
            if (visual == null)
            {
                throw new InvalidOperationException("Player prefab must contain a WeaponVisualController (including inactive children).");
            }

            Player playerData = playerInstance.GetComponentInChildren<Player>(true);
            if (playerData == null)
            {
                throw new InvalidOperationException("Player prefab must contain a Player (including inactive children).");
            }

            PlayerWeaponController playerWeaponController = playerInstance.GetComponentInChildren<PlayerWeaponController>(true);
            if (playerWeaponController == null)
            {
                playerWeaponController = playerData.gameObject.AddComponent<PlayerWeaponController>();
            }

            IReadOnlyList<AssetReference> weaponRefs = loadout.WeaponPrefabs;
            int count = weaponRefs.Count;
            var spawned = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                AddressablePrefabSpawn weaponSpawn = await InstantiateWeaponAtSocketAsync(weaponRefs[i], visual, cancellationToken);
                spawned.Add(weaponSpawn.Instance);
                acquiredHandles.Add(weaponSpawn.Handle);
            }

            visual.SetWeaponInstances(spawned);

            playerWeaponController.Bind(playerData, visual);

            playerData.SetAvailableWeapons(spawned);
            return playerData;
        }

        private async Task<AddressablePrefabSpawn> InstantiatePlayerFromReferenceAsync(
            AssetReference playerReference,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            CancellationToken cancellationToken)
        {
            IAssetHandle<GameObject> handle = await gateway.LoadAsync<GameObject>(playerReference, cancellationToken);
            try
            {
                GameObject prefab = handle.Asset;
                GameObject instance = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
                return new AddressablePrefabSpawn(instance, handle);
            }
            catch
            {
                if (!handle.IsReleased)
                {
                    handle.Release();
                }

                throw;
            }
        }

        private async Task<AddressablePrefabSpawn> InstantiateWeaponAtSocketAsync(
            AssetReference weaponReference,
            WeaponVisualController visual,
            CancellationToken cancellationToken)
        {
            Transform socket = visual.WeaponSocket;
            if (socket == null)
            {
                throw new InvalidOperationException("Weapon socket is not assigned on WeaponVisualController.");
            }

            IAssetHandle<GameObject> handle = await gateway.LoadAsync<GameObject>(weaponReference, cancellationToken);
            try
            {
                GameObject prefab = handle.Asset;
                GameObject instance = UnityEngine.Object.Instantiate(prefab, socket);
                return new AddressablePrefabSpawn(instance, handle);
            }
            catch
            {
                if (!handle.IsReleased)
                {
                    handle.Release();
                }

                throw;
            }
        }
    }
}
