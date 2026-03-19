using System;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Scaffold.Maps;
using UnityEngine;

namespace Madbox.Addressables
{
    internal sealed class AddressablesLeaseStore
    {
        public AddressablesLeaseStore(IAddressablesAssetClient client)
        {
            if (client == null) { throw new ArgumentNullException(nameof(client)); }
            this.client = client;
        }

        private readonly IAddressablesAssetClient client;
        private readonly Map<Type, string, AddressablesLoadedEntry> loaded = new Map<Type, string, AddressablesLoadedEntry>();
        private readonly object sync = new object();

        public async Task<IAssetHandle<T>> AcquireAsync<T>(string key, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            Type assetType = typeof(T);
            GuardAcquireRequest(assetType, key, cancellationToken);
            if (TryAcquireLoadedEntry(assetType, key, PreloadMode.Normal, false, out AddressablesLoadedEntry existing))
            {
                return CreateTypedHandle<T>(key, existing.Asset);
            }

            return await LoadAndCreateTypedHandleAsync<T>(assetType, key, cancellationToken);
        }

        public async Task<IAssetHandle> AcquireByTypeAsync(Type assetType, string key, PreloadMode preloadMode, bool isPreload, CancellationToken cancellationToken)
        {
            GuardAcquireRequest(assetType, key, cancellationToken);
            if (TryAcquireLoadedEntry(assetType, key, preloadMode, isPreload, out AddressablesLoadedEntry existing)) { return CreateHandle(assetType, key, existing.Asset); }
            return await LoadAndCreateHandleAsync(assetType, key, preloadMode, isPreload, cancellationToken);
        }

        private bool TryAcquireLoadedEntry(Type assetType, string key, PreloadMode preloadMode, bool isPreload, out AddressablesLoadedEntry entry)
        {
            lock (sync)
            {
                if (!TryGetLoadedEntry(assetType, key, out entry)) { return false; }
                ApplyAcquirePolicy(entry, preloadMode, isPreload);
                return true;
            }
        }

        private IAssetHandle CreateHandle(Type assetType, string key, UnityEngine.Object asset)
        {
            string id = CreateId(assetType, key);
            return new AssetHandle<UnityEngine.Object>(id, asset, () => Release(assetType, key));
        }

        private IAssetHandle<T> CreateTypedHandle<T>(string key, UnityEngine.Object asset) where T : UnityEngine.Object
        {
            if (asset is not T typed)
            {
                throw new InvalidOperationException($"Loaded asset type mismatch. Requested '{typeof(T).FullName}', actual '{asset?.GetType().FullName ?? "null"}'.");
            }

            string id = CreateId(typeof(T), key);
            return new AssetHandle<T>(id, typed, () => Release(typeof(T), key));
        }

        private void Release(Type assetType, string key)
        {
            if (!TryRemoveReleasableEntry(assetType, key, out AddressablesLoadedEntry entry)) { return; }
            client.Release(entry.Asset);
        }

        private string CreateId(Type assetType, string key)
        {
            return $"{assetType.FullName}|{key}";
        }

        private void GuardAcquireRequest(Type assetType, string key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GuardAssetType(assetType);
            GuardKey(key);
        }

        private void GuardAssetType(Type assetType)
        {
            if (assetType == null) { throw new ArgumentNullException(nameof(assetType)); }
            if (!typeof(UnityEngine.Object).IsAssignableFrom(assetType)) { throw new ArgumentException($"Asset type '{assetType.FullName}' must inherit UnityEngine.Object.", nameof(assetType)); }
        }

        private void GuardKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentException("Asset key cannot be empty.", nameof(key)); }
        }

        private bool TryGetLoadedEntry(Type assetType, string key, out AddressablesLoadedEntry entry)
        {
            return loaded.TryGetValue(assetType, key, out entry);
        }

        private void ApplyAcquirePolicy(AddressablesLoadedEntry entry, PreloadMode preloadMode, bool isPreload)
        {
            if (preloadMode == PreloadMode.NeverDie) { entry.Policy = PreloadMode.NeverDie; }
            if (!isPreload) { entry.RefCount++; }
        }

        private async Task<IAssetHandle<T>> LoadAndCreateTypedHandleAsync<T>(Type assetType, string key, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            UnityEngine.Object loadedAsset = await client.LoadAssetAsync(key, assetType, cancellationToken);
            AddressablesLoadedEntry created = AddNewEntry(assetType, key, loadedAsset, PreloadMode.Normal, false);
            created.RefCount = 1;
            return CreateTypedHandle<T>(key, created.Asset);
        }

        private async Task<IAssetHandle> LoadAndCreateHandleAsync(Type assetType, string key, PreloadMode preloadMode, bool isPreload, CancellationToken cancellationToken)
        {
            UnityEngine.Object loadedAsset = await client.LoadAssetAsync(key, assetType, cancellationToken);
            AddressablesLoadedEntry entry = AddNewEntry(assetType, key, loadedAsset, preloadMode, isPreload);
            if (!isPreload) { entry.RefCount = 1; }
            return CreateHandle(assetType, key, loadedAsset);
        }

        private AddressablesLoadedEntry AddNewEntry(Type assetType, string key, UnityEngine.Object asset, PreloadMode preloadMode, bool isPreload)
        {
            lock (sync)
            {
                if (TryGetLoadedEntry(assetType, key, out AddressablesLoadedEntry existing)) { return AcquireExistingEntry(existing, preloadMode, isPreload); }
                return CreateAndStoreEntry(assetType, key, asset, preloadMode);
            }
        }

        private AddressablesLoadedEntry AcquireExistingEntry(AddressablesLoadedEntry existing, PreloadMode preloadMode, bool isPreload)
        {
            ApplyAcquirePolicy(existing, preloadMode, isPreload);
            return existing;
        }

        private AddressablesLoadedEntry CreateAndStoreEntry(Type assetType, string key, UnityEngine.Object asset, PreloadMode preloadMode)
        {
            AddressablesLoadedEntry created = new AddressablesLoadedEntry(asset, preloadMode);
            loaded.Add(assetType, key, created);
            return created;
        }

        private bool TryRemoveReleasableEntry(Type assetType, string key, out AddressablesLoadedEntry entry)
        {
            lock (sync)
            {
                if (!TryGetLoadedEntry(assetType, key, out entry)) { return false; }
                DecrementRefCount(entry);
                if (!CanRelease(entry)) { return false; }
                loaded.Remove(assetType, key);
                return true;
            }
        }

        private void DecrementRefCount(AddressablesLoadedEntry entry)
        {
            if (entry.RefCount > 0) { entry.RefCount--; }
        }

        private bool CanRelease(AddressablesLoadedEntry entry)
        {
            if (entry.RefCount > 0) { return false; }
            return entry.Policy != PreloadMode.NeverDie;
        }
    }
}
