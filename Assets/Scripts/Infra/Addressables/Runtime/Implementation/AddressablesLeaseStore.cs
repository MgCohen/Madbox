using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using UnityEngine;

namespace Madbox.Addressables
{
    internal sealed class AddressablesLeaseStore
    {
        public AddressablesLeaseStore(IAddressablesAssetClient client)
        {
            GuardClient(client);
            this.client = client;
        }

        private readonly IAddressablesAssetClient client;
        private readonly Dictionary<AddressablesLoadToken, AddressablesLoadedEntry> loaded = new Dictionary<AddressablesLoadToken, AddressablesLoadedEntry>();
        private readonly Dictionary<AddressablesLoadToken, IAssetHandle> normalPreloaded = new Dictionary<AddressablesLoadToken, IAssetHandle>();

        public async Task<IAssetHandle<T>> AcquireAsync<T>(AddressablesLoadToken token, System.Threading.CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            GuardAcquire(token);
            cancellationToken.ThrowIfCancellationRequested();
            IAssetHandle<T> preloaded = TryTakeNormalPreloadedTyped<T>(token, token.Key);
            if (preloaded != null) { return preloaded; }
            if (TryAcquireExisting(token, out IAssetHandle<T> existing)) { return existing; }
            T loadedAsset = await client.LoadAssetAsync<T>(token.Key, cancellationToken);
            loaded[token] = new AddressablesLoadedEntry(loadedAsset);
            return CreateHandle(token, loadedAsset);
        }

        public async Task PreloadByTypeAsync(Type assetType, AssetKey key, PreloadMode mode, System.Threading.CancellationToken cancellationToken)
        {
            GuardPreload(assetType);
            MethodInfo method = typeof(AddressablesLeaseStore).GetMethod(nameof(PreloadTypedAsync), BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo genericMethod = method.MakeGenericMethod(assetType);
            Task task = (Task)genericMethod.Invoke(this, new object[] { key, mode, cancellationToken });
            await task;
        }

        private async Task PreloadTypedAsync<T>(AssetKey key, PreloadMode mode, System.Threading.CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            AddressablesLoadToken token = CreateToken<T>(key);
            IAssetHandle<T> handle = await AcquireAsync<T>(token, cancellationToken);
            StorePreloadHandle(token, handle, mode);
        }

        private AddressablesLoadToken CreateToken<T>(AssetKey key) where T : UnityEngine.Object
        {
            return new AddressablesLoadToken(typeof(T), key);
        }

        private bool TryAcquireExisting<T>(AddressablesLoadToken token, out IAssetHandle<T> handle) where T : UnityEngine.Object
        {
            if (!loaded.TryGetValue(token, out AddressablesLoadedEntry entry)) { handle = null; return false; }
            entry.RefCount++;
            T existingAsset = CastAsset<T>(entry.Asset);
            handle = CreateHandle(token, existingAsset);
            return true;
        }

        private T CastAsset<T>(UnityEngine.Object asset) where T : UnityEngine.Object
        {
            if (asset is T typed) { return typed; }
            throw new InvalidOperationException($"Loaded asset type mismatch. Requested '{typeof(T).FullName}', actual '{asset?.GetType().FullName ?? "null"}'.");
        }

        private IAssetHandle<T> CreateHandle<T>(AddressablesLoadToken token, T asset) where T : UnityEngine.Object
        {
            return new AssetHandle<T>(token.Id, asset, () => Release(token));
        }

        private IAssetHandle<T> TryTakeNormalPreloadedTyped<T>(AddressablesLoadToken token, AssetKey key) where T : UnityEngine.Object
        {
            if (!TryTakeNormalPreloaded(token, out IAssetHandle handle)) { return null; }
            return CastPreloadedHandle<T>(handle, key);
        }

        private bool TryTakeNormalPreloaded(AddressablesLoadToken token, out IAssetHandle handle)
        {
            bool found = normalPreloaded.TryGetValue(token, out handle);
            if (found) { normalPreloaded.Remove(token); }
            return found;
        }

        private IAssetHandle<T> CastPreloadedHandle<T>(IAssetHandle handle, AssetKey key) where T : UnityEngine.Object
        {
            if (handle is IAssetHandle<T> typed) { return typed; }
            throw new InvalidOperationException($"Preloaded handle type mismatch for key '{key.Value}'.");
        }

        private void StorePreloadHandle(AddressablesLoadToken token, IAssetHandle handle, PreloadMode mode)
        {
            if (mode == PreloadMode.NeverDie) { return; }
            if (normalPreloaded.ContainsKey(token)) { handle.Release(); return; }
            normalPreloaded[token] = handle;
        }

        private void Release(AddressablesLoadToken token)
        {
            if (!loaded.TryGetValue(token, out AddressablesLoadedEntry entry)) { return; }
            if (entry.RefCount > 0) { entry.RefCount--; }
            if (entry.RefCount > 0) { return; }
            loaded.Remove(token);
            client.Release(entry.Asset);
        }

        private void GuardClient(IAddressablesAssetClient client)
        {
            if (client == null) { throw new ArgumentNullException(nameof(client)); }
        }

        private void GuardAcquire(AddressablesLoadToken token)
        {
            if (string.IsNullOrWhiteSpace(token.Id)) { throw new ArgumentException("Load token must be valid.", nameof(token)); }
        }

        private void GuardPreload(Type assetType)
        {
            if (assetType == null) { throw new ArgumentNullException(nameof(assetType)); }
        }
    }
}
