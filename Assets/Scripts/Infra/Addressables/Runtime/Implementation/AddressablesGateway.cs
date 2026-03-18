using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Scaffold.Addressables.Contracts;
using UnityEngine;

namespace Scaffold.Addressables
{
    public sealed class AddressablesGateway : IAddressablesGateway
    {
        private readonly IAddressablesAssetClient client;
        private readonly IAddressablesPreloadSource preloadSource;
        private readonly Dictionary<AddressablesLoadToken, AddressablesLoadedEntry> loaded = new Dictionary<AddressablesLoadToken, AddressablesLoadedEntry>();
        private readonly Dictionary<AddressablesLoadToken, IAssetHandle> normalPreloaded = new Dictionary<AddressablesLoadToken, IAssetHandle>();
        private readonly SemaphoreSlim operationLock = new SemaphoreSlim(1, 1);
        private readonly object initSync = new object();
        private Task initializeTask = Task.CompletedTask;
        private bool initialized;

        public AddressablesGateway(IAddressablesAssetClient client, IAddressablesPreloadRegistry preloadRegistry)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            preloadSource = preloadRegistry as IAddressablesPreloadSource
                ?? throw new ArgumentException("Preload registry must implement internal preload source contract.", nameof(preloadRegistry));
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            lock (initSync)
            {
                if (initialized)
                {
                    return Task.CompletedTask;
                }

                if (!initializeTask.IsCompleted)
                {
                    return initializeTask;
                }

                initializeTask = InitializeInternalAsync(cancellationToken);
                return initializeTask;
            }
        }

        public async Task<IAssetHandle<T>> LoadAsync<T>(AssetKey key, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            AddressablesLoadToken token = new AddressablesLoadToken(typeof(T), key);
            if (TryTakeNormalPreloaded(token, out IAssetHandle preloadedHandle))
            {
                if (preloadedHandle is IAssetHandle<T> typedPreloadedHandle)
                {
                    return typedPreloadedHandle;
                }

                throw new InvalidOperationException($"Preloaded handle type mismatch for key '{key.Value}'.");
            }

            return await LoadTypedInternalAsync<T>(token, cancellationToken);
        }

        public async Task<IReadOnlyList<IAssetHandle<T>>> LoadAsync<T>(CatalogKey catalog, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            IReadOnlyList<AssetKey> keys = await client.ResolveCatalogAsync(typeof(T), catalog, cancellationToken);
            List<IAssetHandle<T>> handles = new List<IAssetHandle<T>>(keys.Count);
            foreach (AssetKey key in keys)
            {
                handles.Add(await LoadAsync<T>(key, cancellationToken));
            }

            return handles;
        }

        public Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceKey reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            return LoadAsync<T>(new AssetKey(reference.Value), cancellationToken);
        }

        internal async Task PreloadAsync<T>(AssetKey key, PreloadMode mode, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            IAssetHandle<T> handle = await LoadAsync<T>(key, cancellationToken);
            AddressablesLoadToken token = new AddressablesLoadToken(typeof(T), key);
            StorePreloadHandle(token, handle, mode);
        }

        private async Task InitializeInternalAsync(CancellationToken cancellationToken)
        {
            await TrySyncCatalogAndContentAsync(cancellationToken);
            IReadOnlyList<AddressablesPreloadRequest> requests = preloadSource.Snapshot();
            foreach (AddressablesPreloadRequest request in requests)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ApplyPreloadRequestAsync(request, cancellationToken);
            }

            lock (initSync)
            {
                initialized = true;
            }
        }

        private async Task TrySyncCatalogAndContentAsync(CancellationToken cancellationToken)
        {
            try
            {
                await client.SyncCatalogAndContentAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Addressables catalog/content sync failed. Continuing startup. {exception.GetType().Name}: {exception.Message}");
            }
        }

        private async Task ApplyPreloadRequestAsync(AddressablesPreloadRequest request, CancellationToken cancellationToken)
        {
            if (request.IsCatalog)
            {
                IReadOnlyList<AssetKey> keys = await client.ResolveCatalogAsync(request.AssetType, request.Catalog, cancellationToken);
                foreach (AssetKey key in keys)
                {
                    await PreloadByTypeAsync(request.AssetType, key, request.Mode, cancellationToken);
                }

                return;
            }

            await PreloadByTypeAsync(request.AssetType, request.Key, request.Mode, cancellationToken);
        }

        private async Task PreloadByTypeAsync(Type assetType, AssetKey key, PreloadMode mode, CancellationToken cancellationToken)
        {
            MethodInfo method = typeof(AddressablesGateway).GetMethod(nameof(PreloadTypedByTypeAsync), BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo genericMethod = method.MakeGenericMethod(assetType);
            Task task = (Task)genericMethod.Invoke(this, new object[] { key, mode, cancellationToken });
            await task;
        }

        private async Task PreloadTypedByTypeAsync<T>(AssetKey key, PreloadMode mode, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            await PreloadAsync<T>(key, mode, cancellationToken);
        }

        private void StorePreloadHandle(AddressablesLoadToken token, IAssetHandle handle, PreloadMode mode)
        {
            if (mode == PreloadMode.NeverDie) { return; }

            lock (normalPreloaded)
            {
                if (normalPreloaded.ContainsKey(token))
                {
                    handle.Release();
                    return;
                }

                normalPreloaded[token] = handle;
            }
        }

        private bool TryTakeNormalPreloaded(AddressablesLoadToken token, out IAssetHandle handle)
        {
            lock (normalPreloaded)
            {
                if (!normalPreloaded.TryGetValue(token, out handle))
                {
                    return false;
                }

                normalPreloaded.Remove(token);
                return true;
            }
        }

        private async Task<IAssetHandle<T>> LoadTypedInternalAsync<T>(AddressablesLoadToken token, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            await operationLock.WaitAsync(cancellationToken);
            try
            {
                if (TryGetExistingEntry(token, out AddressablesLoadedEntry existing))
                {
                    existing.RefCount++;
                    return CreateHandle<T>(token, CastAsset<T>(existing.Asset));
                }

                T loadedAsset = await client.LoadAssetAsync<T>(token.Key, cancellationToken);
                loaded[token] = new AddressablesLoadedEntry(loadedAsset);
                return CreateHandle(token, loadedAsset);
            }
            finally
            {
                operationLock.Release();
            }
        }

        private bool TryGetExistingEntry(AddressablesLoadToken token, out AddressablesLoadedEntry existing)
        {
            return loaded.TryGetValue(token, out existing);
        }

        private T CastAsset<T>(UnityEngine.Object asset) where T : UnityEngine.Object
        {
            if (asset is T typed)
            {
                return typed;
            }

            throw new InvalidOperationException($"Loaded asset type mismatch. Requested '{typeof(T).FullName}', actual '{asset?.GetType().FullName ?? "null"}'.");
        }

        private IAssetHandle<T> CreateHandle<T>(AddressablesLoadToken token, T asset) where T : UnityEngine.Object
        {
            return new AssetHandle<T>(token.Id, asset, () => ReleaseToken(token));
        }

        private void ReleaseToken(AddressablesLoadToken token)
        {
            operationLock.Wait();
            try
            {
                if (!loaded.TryGetValue(token, out AddressablesLoadedEntry entry))
                {
                    return;
                }

                if (entry.RefCount > 0)
                {
                    entry.RefCount--;
                }

                if (entry.RefCount > 0)
                {
                    return;
                }

                loaded.Remove(token);
                client.Release(entry.Asset);
            }
            finally
            {
                operationLock.Release();
            }
        }
    }
}
