using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Scaffold.Addressables.Contracts;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Scaffold.Addressables
{
    public sealed class AddressablesAssetClient : IAddressablesAssetClient
    {
        public async Task SyncCatalogAndContentAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var checkHandle = UnityEngine.AddressableAssets.Addressables.CheckForCatalogUpdates(false);
            IList<string> catalogs = await checkHandle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(checkHandle);
            cancellationToken.ThrowIfCancellationRequested();

            if (catalogs == null || catalogs.Count == 0)
            {
                return;
            }

            var updateHandle = UnityEngine.AddressableAssets.Addressables.UpdateCatalogs(catalogs, false);
            await updateHandle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(updateHandle);
            cancellationToken.ThrowIfCancellationRequested();

            foreach (string catalog in catalogs)
            {
                var sizeHandle = UnityEngine.AddressableAssets.Addressables.GetDownloadSizeAsync(catalog);
                long downloadSize = await sizeHandle.Task;
                UnityEngine.AddressableAssets.Addressables.Release(sizeHandle);
                cancellationToken.ThrowIfCancellationRequested();

                if (downloadSize <= 0)
                {
                    continue;
                }

                var downloadHandle = UnityEngine.AddressableAssets.Addressables.DownloadDependenciesAsync(catalog, UnityEngine.AddressableAssets.Addressables.MergeMode.Union, false);
                await downloadHandle.Task;
                UnityEngine.AddressableAssets.Addressables.Release(downloadHandle);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        public async Task<T> LoadAssetAsync<T>(AssetKey key, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            cancellationToken.ThrowIfCancellationRequested();
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(key.Value);
            T asset = await handle.Task;
            cancellationToken.ThrowIfCancellationRequested();

            if (asset == null)
            {
                throw new InvalidOperationException($"Addressables returned null for key '{key.Value}' and type '{typeof(T).FullName}'.");
            }

            return asset;
        }

        public async Task<IReadOnlyList<AssetKey>> ResolveCatalogAsync(Type assetType, CatalogKey catalog, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var locationsHandle = UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(catalog.Value, assetType);
            IList<IResourceLocation> locations = await locationsHandle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(locationsHandle);
            cancellationToken.ThrowIfCancellationRequested();

            HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
            List<AssetKey> keys = new List<AssetKey>();
            foreach (IResourceLocation location in locations)
            {
                if (location == null || string.IsNullOrWhiteSpace(location.PrimaryKey))
                {
                    continue;
                }

                if (!unique.Add(location.PrimaryKey))
                {
                    continue;
                }

                keys.Add(new AssetKey(location.PrimaryKey));
            }

            return keys;
        }

        public void Release(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return;
            }

            UnityEngine.AddressableAssets.Addressables.Release(asset);
        }
    }
}
