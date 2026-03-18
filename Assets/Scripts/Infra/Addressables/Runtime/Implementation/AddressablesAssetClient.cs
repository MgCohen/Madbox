using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Madbox.Addressables
{
    public sealed class AddressablesAssetClient : IAddressablesAssetClient
    {
        public async Task SyncCatalogAndContentAsync(CancellationToken cancellationToken)
        {
            GuardCancellation(cancellationToken);
            IList<string> catalogs = await CheckForCatalogUpdatesAsync(cancellationToken);
            if (HasNoCatalogs(catalogs)) { return; }
            await UpdateCatalogsAsync(catalogs, cancellationToken);
            await DownloadCatalogDependenciesAsync(catalogs, cancellationToken);
        }

        public async Task<T> LoadAssetAsync<T>(AssetKey key, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            GuardCancellation(cancellationToken);
            T asset = await LoadTypedAssetAsync<T>(key.Value, cancellationToken);
            EnsureAssetWasLoaded(asset, key);
            return asset;
        }

        public async Task<IReadOnlyList<AssetKey>> ResolveLabelAsync(Type assetType, AssetLabelReference label, CancellationToken cancellationToken)
        {
            GuardCancellation(cancellationToken);
            GuardLabel(label);
            IList<IResourceLocation> locations = await LoadLocationsAsync(assetType, label, cancellationToken);
            return ToDistinctAssetKeys(locations);
        }

        public void Release(UnityEngine.Object asset)
        {
            if (asset == null) { return; }
            UnityEngine.AddressableAssets.Addressables.Release(asset);
        }

        private async Task<IList<string>> CheckForCatalogUpdatesAsync(CancellationToken cancellationToken)
        {
            var handle = UnityEngine.AddressableAssets.Addressables.CheckForCatalogUpdates(false);
            IList<string> catalogs = await handle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(handle);
            GuardCancellation(cancellationToken);
            return catalogs;
        }

        private bool HasNoCatalogs(IList<string> catalogs)
        {
            return catalogs == null || catalogs.Count == 0;
        }

        private async Task UpdateCatalogsAsync(IList<string> catalogs, CancellationToken cancellationToken)
        {
            var handle = UnityEngine.AddressableAssets.Addressables.UpdateCatalogs(catalogs, false);
            await handle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(handle);
            GuardCancellation(cancellationToken);
        }

        private async Task DownloadCatalogDependenciesAsync(IList<string> catalogs, CancellationToken cancellationToken)
        {
            foreach (string catalog in catalogs)
            {
                await DownloadCatalogIfNeededAsync(catalog, cancellationToken);
            }
        }

        private async Task DownloadCatalogIfNeededAsync(string catalog, CancellationToken cancellationToken)
        {
            long size = await GetDownloadSizeAsync(catalog, cancellationToken);
            if (size <= 0) { return; }
            await DownloadDependenciesAsync(catalog, cancellationToken);
        }

        private async Task<long> GetDownloadSizeAsync(string catalog, CancellationToken cancellationToken)
        {
            var sizeHandle = UnityEngine.AddressableAssets.Addressables.GetDownloadSizeAsync(catalog);
            long downloadSize = await sizeHandle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(sizeHandle);
            GuardCancellation(cancellationToken);
            return downloadSize;
        }

        private async Task DownloadDependenciesAsync(string catalog, CancellationToken cancellationToken)
        {
            var handle = UnityEngine.AddressableAssets.Addressables.DownloadDependenciesAsync(catalog, UnityEngine.AddressableAssets.Addressables.MergeMode.Union, false);
            await handle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(handle);
            GuardCancellation(cancellationToken);
        }

        private async Task<T> LoadTypedAssetAsync<T>(string keyValue, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(keyValue);
            T asset = await handle.Task;
            GuardCancellation(cancellationToken);
            return asset;
        }

        private void EnsureAssetWasLoaded<T>(T asset, AssetKey key) where T : UnityEngine.Object
        {
            if (asset == null)
            {
                throw new InvalidOperationException($"Addressables returned null for key '{key.Value}' and type '{typeof(T).FullName}'.");
            }
        }

        private async Task<IList<IResourceLocation>> LoadLocationsAsync(Type assetType, AssetLabelReference label, CancellationToken cancellationToken)
        {
            var handle = UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(label, assetType);
            IList<IResourceLocation> locations = await handle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(handle);
            GuardCancellation(cancellationToken);
            return locations;
        }

        private IReadOnlyList<AssetKey> ToDistinctAssetKeys(IList<IResourceLocation> locations)
        {
            HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
            List<AssetKey> keys = new List<AssetKey>();
            foreach (IResourceLocation location in locations) { AddDistinctLocationKey(location, unique, keys); }
            return keys;
        }

        private void AddDistinctLocationKey(IResourceLocation location, ISet<string> unique, ICollection<AssetKey> keys)
        {
            if (!ShouldIncludeLocation(location, unique)) { return; }
            AssetKey key = CreateAssetKey(location.PrimaryKey);
            keys.Add(key);
        }

        private bool ShouldIncludeLocation(IResourceLocation location, ISet<string> uniqueKeys)
        {
            if (location == null) { return false; }
            if (string.IsNullOrWhiteSpace(location.PrimaryKey)) { return false; }
            return uniqueKeys.Add(location.PrimaryKey);
        }

        private AssetKey CreateAssetKey(string key)
        {
            return new AssetKey(key);
        }

        private void GuardCancellation(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        private void GuardLabel(AssetLabelReference label)
        {
            if (label == null || string.IsNullOrWhiteSpace(label.labelString)) { throw new ArgumentException("Label reference cannot be empty.", nameof(label)); }
        }
    }
}
