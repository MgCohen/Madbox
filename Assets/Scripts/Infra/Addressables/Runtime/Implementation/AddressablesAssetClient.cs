using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
#pragma warning disable SCA0006

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

        public async Task<UnityEngine.Object> LoadAssetAsync(string key, Type assetType, CancellationToken cancellationToken)
        {
            GuardCancellation(cancellationToken);
            GuardKey(key);
            GuardAssetType(assetType);
            bool hasLocation = await HasLocationAsync(key, assetType, cancellationToken);
            if (!hasLocation)
            {
                throw new InvalidOperationException($"Addressables key '{key}' with type '{assetType.FullName}' was not found in resource locations.");
            }

            UnityEngine.Object asset = await LoadAssetCoreAsync(key, cancellationToken);
            EnsureAssetWasLoaded(asset, key, assetType);
            return asset;
        }

        public async Task<IReadOnlyList<string>> ResolveLabelAsync(Type assetType, AssetLabelReference label, CancellationToken cancellationToken)
        {
            GuardCancellation(cancellationToken);
            GuardAssetType(assetType);
            GuardLabel(label);
            IList<IResourceLocation> locations = await LoadLocationsAsync(assetType, label, cancellationToken);
            return ToDistinctKeys(locations);
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

        private async Task<UnityEngine.Object> LoadAssetCoreAsync(string keyValue, CancellationToken cancellationToken)
        {
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<UnityEngine.Object>(keyValue);
            UnityEngine.Object asset = await handle.Task;
            GuardCancellation(cancellationToken);
            return asset;
        }

        private void EnsureAssetWasLoaded(UnityEngine.Object asset, string key, Type assetType)
        {
            if (asset == null)
            {
                throw new InvalidOperationException($"Addressables returned null for key '{key}' and type '{assetType.FullName}'.");
            }

            if (!assetType.IsInstanceOfType(asset))
            {
                throw new InvalidOperationException($"Addressables loaded type '{asset.GetType().FullName}' for key '{key}', expected assignable to '{assetType.FullName}'.");
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

        private async Task<bool> HasLocationAsync(string key, Type assetType, CancellationToken cancellationToken)
        {
            var handle = UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(key, assetType);
            IList<IResourceLocation> locations = await handle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(handle);
            GuardCancellation(cancellationToken);
            return locations != null && locations.Count > 0;
        }

        private IReadOnlyList<string> ToDistinctKeys(IList<IResourceLocation> locations)
        {
            HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
            List<string> keys = new List<string>();
            foreach (IResourceLocation location in locations) { AddDistinctLocationKey(location, unique, keys); }
            return keys;
        }

        private void AddDistinctLocationKey(IResourceLocation location, ISet<string> unique, ICollection<string> keys)
        {
            if (!ShouldIncludeLocation(location, unique)) { return; }
            keys.Add(location.PrimaryKey);
        }

        private bool ShouldIncludeLocation(IResourceLocation location, ISet<string> uniqueKeys)
        {
            if (location == null) { return false; }
            if (string.IsNullOrWhiteSpace(location.PrimaryKey)) { return false; }
            return uniqueKeys.Add(location.PrimaryKey);
        }

        private void GuardKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentException("Asset key cannot be empty.", nameof(key)); }
        }

        private void GuardCancellation(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        private void GuardAssetType(Type assetType)
        {
            if (assetType == null) { throw new ArgumentNullException(nameof(assetType)); }
            if (!typeof(UnityEngine.Object).IsAssignableFrom(assetType)) { throw new ArgumentException($"Asset type '{assetType.FullName}' must inherit UnityEngine.Object.", nameof(assetType)); }
        }

        private void GuardLabel(AssetLabelReference label)
        {
            if (label == null || string.IsNullOrWhiteSpace(label.labelString)) { throw new ArgumentException("Label reference cannot be empty.", nameof(label)); }
        }
    }
}
#pragma warning restore SCA0006
