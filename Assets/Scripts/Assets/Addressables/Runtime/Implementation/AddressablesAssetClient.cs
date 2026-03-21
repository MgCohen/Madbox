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
            if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);
            var checkHandle = UnityEngine.AddressableAssets.Addressables.CheckForCatalogUpdates(false);
            IList<string> catalogs = await checkHandle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(checkHandle);
            cancellationToken.ThrowIfCancellationRequested();
            if (catalogs == null || catalogs.Count == 0) return;
            var updateHandle = UnityEngine.AddressableAssets.Addressables.UpdateCatalogs(catalogs, false);
            await updateHandle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(updateHandle);
            cancellationToken.ThrowIfCancellationRequested();
            await DownloadCatalogDependenciesAsync(catalogs, cancellationToken);
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
            var sizeHandle = UnityEngine.AddressableAssets.Addressables.GetDownloadSizeAsync(catalog);
            long size = await sizeHandle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(sizeHandle);
            cancellationToken.ThrowIfCancellationRequested();
            if (size <= 0) return;
            await DownloadDependenciesAsync(catalog, cancellationToken);
        }

        private async Task DownloadDependenciesAsync(string catalog, CancellationToken cancellationToken)
        {
            var handle = UnityEngine.AddressableAssets.Addressables.DownloadDependenciesAsync(catalog, UnityEngine.AddressableAssets.Addressables.MergeMode.Union, false);
            await handle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(handle);
            cancellationToken.ThrowIfCancellationRequested();
        }

        public async Task<UnityEngine.Object> LoadAssetAsync(string key, Type assetType, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Asset key cannot be empty.", nameof(key));
            if (assetType == null) throw new ArgumentNullException(nameof(assetType));
            if (!typeof(UnityEngine.Object).IsAssignableFrom(assetType)) throw new ArgumentException($"Asset type '{assetType.FullName}' must inherit UnityEngine.Object.", nameof(assetType));
            var locationsHandle = UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(key, assetType); IList<IResourceLocation> locations = await locationsHandle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(locationsHandle); cancellationToken.ThrowIfCancellationRequested();
            if (locations == null || locations.Count == 0) throw new InvalidOperationException($"Addressables key '{key}' with type '{assetType.FullName}' was not found in resource locations.");
            var assetHandle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<UnityEngine.Object>(key); UnityEngine.Object asset = await assetHandle.Task;
            cancellationToken.ThrowIfCancellationRequested();
            EnsureAssetWasLoaded(asset, key, assetType);
            return asset;
        }

        public async Task<IReadOnlyList<UnityEngine.Object>> LoadAssetsByLabelAsync(Type assetType, AssetLabelReference label, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);
            if (assetType == null) throw new ArgumentNullException(nameof(assetType));
            if (!typeof(UnityEngine.Object).IsAssignableFrom(assetType)) throw new ArgumentException($"Asset type '{assetType.FullName}' must inherit UnityEngine.Object.", nameof(assetType));
            if (label == null || string.IsNullOrWhiteSpace(label.labelString)) throw new ArgumentException("Label reference cannot be empty.", nameof(label));

            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync<UnityEngine.Object>(
                label.labelString,
                null,
                UnityEngine.AddressableAssets.Addressables.MergeMode.Union,
                false);
            IList<UnityEngine.Object> loadedAssets = await handle.Task;
            cancellationToken.ThrowIfCancellationRequested();
            return ToValidatedTypedAssets(loadedAssets, label.labelString, assetType);
        }

        private IReadOnlyList<UnityEngine.Object> ToValidatedTypedAssets(IList<UnityEngine.Object> loadedAssets, string label, Type assetType)
        {
            List<UnityEngine.Object> validated = new List<UnityEngine.Object>();
            if (loadedAssets == null)
            {
                return validated;
            }

            for (int i = 0; i < loadedAssets.Count; i++)
            {
                UnityEngine.Object asset = loadedAssets[i];
                if (asset == null)
                {
                    continue;
                }

                if (!assetType.IsInstanceOfType(asset))
                {
                    throw new InvalidOperationException(
                        $"Addressables loaded type '{asset.GetType().FullName}' for label '{label}', expected assignable to '{assetType.FullName}'.");
                }

                validated.Add(asset);
            }

            return validated;
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

        public async Task<IReadOnlyList<string>> ResolveLabelAsync(Type assetType, AssetLabelReference label, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);
            if (assetType == null) throw new ArgumentNullException(nameof(assetType));
            if (!typeof(UnityEngine.Object).IsAssignableFrom(assetType)) throw new ArgumentException($"Asset type '{assetType.FullName}' must inherit UnityEngine.Object.", nameof(assetType));
            if (label == null || string.IsNullOrWhiteSpace(label.labelString)) throw new ArgumentException("Label reference cannot be empty.", nameof(label));
            var locationsHandle = UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(label, assetType);
            IList<IResourceLocation> locations = await locationsHandle.Task;
            UnityEngine.AddressableAssets.Addressables.Release(locationsHandle);
            cancellationToken.ThrowIfCancellationRequested();
            return ToDistinctKeys(locations);
        }

        private IReadOnlyList<string> ToDistinctKeys(IList<IResourceLocation> locations)
        {
            HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
            List<string> keys = new List<string>();
            foreach (IResourceLocation location in locations)
            {
                AddDistinctLocationKey(location, unique, keys);
            }

            return keys;
        }

        private void AddDistinctLocationKey(IResourceLocation location, ISet<string> unique, ICollection<string> keys)
        {
            if (!ShouldIncludeLocation(location, unique))
            {
                return;
            }

            keys.Add(location.PrimaryKey);
        }

        private bool ShouldIncludeLocation(IResourceLocation location, ISet<string> uniqueKeys)
        {
            if (location == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(location.PrimaryKey))
            {
                return false;
            }

            return uniqueKeys.Add(location.PrimaryKey);
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
