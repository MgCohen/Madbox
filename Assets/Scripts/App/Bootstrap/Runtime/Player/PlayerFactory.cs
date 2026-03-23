using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.App.GameView.Player;
using Madbox.Player;
using Madbox.Entities;
using Madbox.Levels;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.App.Bootstrap.Player
{
    public sealed class PlayerFactory
    {
        public PlayerFactory(PlayerService playerService, IAddressablesGateway gateway)
        {
            this.playerService = playerService ?? throw new System.ArgumentNullException(nameof(playerService));
            this.gateway = gateway ?? throw new System.ArgumentNullException(nameof(gateway));
        }

        private readonly PlayerService playerService;

        private readonly IAddressablesGateway gateway;

        public async Task<Player> CreateReadyPlayerAsync(Transform parent, Vector3 position, Quaternion rotation, CancellationToken cancellationToken = default)
        {
            PlayerLoadoutDefinition loadout = playerService.Loadout;
            GameObject playerInstance = null;
            try
            {
                playerInstance = await InstantiatePlayerFromReferenceAsync(loadout.PlayerPrefab, parent, position, rotation, cancellationToken);
                await AttachWeaponsAsync(loadout, playerInstance, cancellationToken);
                Player data = playerInstance.GetComponentInChildren<Player>(true);
                if (data == null)
                {
                    throw new InvalidOperationException("Player prefab must contain a Player (including inactive children).");
                }

                return data;
            }
            catch
            {
                if (playerInstance != null)
                {
                    UnityEngine.Object.Destroy(playerInstance);
                }

                throw;
            }
        }

        private async Task AttachWeaponsAsync(PlayerLoadoutDefinition loadout, GameObject playerInstance, CancellationToken cancellationToken)
        {
            WeaponVisualController visual = playerInstance.GetComponentInChildren<WeaponVisualController>(true);
            Player playerData = playerInstance.GetComponentInChildren<Player>(true);
            PlayerWeaponController playerWeaponController = playerInstance.GetComponentInChildren<PlayerWeaponController>(true);
            IReadOnlyList<AssetReference> weaponRefs = loadout.WeaponPrefabs;
            int count = weaponRefs.Count;
            var spawned = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                spawned.Add(await InstantiateWeaponAtSocketAsync(weaponRefs[i], visual, i, cancellationToken));
            }

            visual.SetWeaponInstances(spawned);

            if (playerWeaponController != null)
            {
                playerWeaponController.Bind(playerData, visual);
            }

            playerData.SetAvailableWeapons(spawned);
        }

        private async Task<GameObject> InstantiatePlayerFromReferenceAsync(
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
                return UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
            }
            finally
            {
                handle.Release();
            }
        }

        private async Task<GameObject> InstantiateWeaponAtSocketAsync(
            AssetReference weaponReference,
            WeaponVisualController visual,
            int socketIndex,
            CancellationToken cancellationToken)
        {
            Transform socket = visual.GetWeaponSocket(socketIndex);
            IAssetHandle<GameObject> handle = await gateway.LoadAsync<GameObject>(weaponReference, cancellationToken);
            try
            {
                GameObject prefab = handle.Asset;
                return UnityEngine.Object.Instantiate(prefab, socket);
            }
            finally
            {
                handle.Release();
            }
        }
    }
}
