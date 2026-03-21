using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.Scope.Contracts;
using UnityEngine.AddressableAssets;
using VContainer;

namespace Madbox.Addressables
{
    public sealed class AddressablesGateway : IAddressablesGateway, IAsyncLayerInitializable, IPreloadedAssetProvider
    {
        private readonly IAddressablesAssetClient client;
        private readonly IAssetReferenceHandler assetReferenceHandler;
        private readonly IAssetPreloadHandler assetPreloadHandler;
        private readonly object initSync = new object();
        private readonly Dictionary<Type, UnityEngine.Object> preloadedAssetsByType = new Dictionary<Type, UnityEngine.Object>();

        private bool initialized;

        public AddressablesGateway(IAddressablesAssetClient client, IAssetReferenceHandler assetReferenceHandler, IAssetPreloadHandler assetPreloadHandler)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.assetReferenceHandler = assetReferenceHandler ?? throw new ArgumentNullException(nameof(assetReferenceHandler));
            this.assetPreloadHandler = assetPreloadHandler ?? throw new ArgumentNullException(nameof(assetPreloadHandler));
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            return InitializeCoreAsync(cancellationToken);
        }

        Task IAsyncLayerInitializable.InitializeAsync(IObjectResolver resolver, CancellationToken cancellationToken)
        {
            return InitializeCoreAsync(cancellationToken);
        }

        public IReadOnlyDictionary<Type, UnityEngine.Object> GetPreloadedAssets()
        {
            lock (initSync)
            {
                return new Dictionary<Type, UnityEngine.Object>(preloadedAssetsByType);
            }
        }

        public async Task<IAssetHandle<T>> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            GuardRuntimeInvariants();
            cancellationToken.ThrowIfCancellationRequested();
            string key = ResolveReferenceKey(reference);
            return await assetReferenceHandler.AcquireAsync<T>(key, cancellationToken);
        }

        public Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            GuardRuntimeInvariants();
            cancellationToken.ThrowIfCancellationRequested();
            return LoadAsync<T>((AssetReference)reference, cancellationToken);
        }

        public async Task<IAssetGroupHandle<T>> LoadAsync<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            GuardRuntimeInvariants();
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<string> keys = await ResolveLabelKeysAsync<T>(label, cancellationToken);
            IReadOnlyList<IAssetHandle<T>> handles = await LoadLabelHandlesAsync<T>(keys, cancellationToken);
            return new AssetGroupHandle<T>(handles);
        }

        public IAssetHandle<T> Load<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            GuardRuntimeInvariants();
            cancellationToken.ThrowIfCancellationRequested();
            string key = ResolveReferenceKey(reference);
            AssetHandle<T> handle = new AssetHandle<T>();
            _ = CompleteLoadAsync(key, handle, cancellationToken);
            return handle;
        }

        public IAssetHandle<T> Load<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            GuardRuntimeInvariants();
            cancellationToken.ThrowIfCancellationRequested();
            return Load<T>((AssetReference)reference, cancellationToken);
        }

        public IAssetGroupHandle<T> Load<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            GuardRuntimeInvariants();
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<string> keys = ResolveLabelKeysAsync<T>(label, cancellationToken).GetAwaiter().GetResult();
            IReadOnlyList<IAssetHandle<T>> handles = LoadLabelHandlesSync<T>(keys, cancellationToken);
            return new AssetGroupHandle<T>(handles);
        }

        private async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (initSync)
            {
                if (initialized)
                {
                    return;
                }
            }

            await RunCatalogSyncAsync(cancellationToken);
            IReadOnlyList<AddressablesPreloadRegistration> preload = await LoadPreloadRegistrationsAsync(cancellationToken);
            await ApplyPreloadAsync(preload, cancellationToken);

            lock (initSync)
            {
                initialized = true;
            }
        }

        private async Task RunCatalogSyncAsync(CancellationToken cancellationToken)
        {
            try
            {
                await client.SyncCatalogAndContentAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                UnityEngine.Debug.LogWarning($"Addressables catalog/content sync failed. Continuing startup. {exception.GetType().Name}: {exception.Message}");
            }
        }

        private async Task<IReadOnlyList<AddressablesPreloadRegistration>> LoadPreloadRegistrationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                UnityEngine.Object raw = await client.LoadAssetAsync(AddressablesPreloadConstants.BootstrapConfigAssetKey, typeof(AddressablesPreloadConfig), cancellationToken);
                AddressablesPreloadConfig config = raw as AddressablesPreloadConfig;
                if (config == null)
                {
                    return Array.Empty<AddressablesPreloadRegistration>();
                }

                return await assetPreloadHandler.BuildAsync(config, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                UnityEngine.Debug.LogWarning($"Addressables preload config '{AddressablesPreloadConstants.BootstrapConfigAssetKey}' was not found or failed to load. Continuing without startup preload. {exception.GetType().Name}: {exception.Message}");
                return Array.Empty<AddressablesPreloadRegistration>();
            }
        }

        private async Task ApplyPreloadAsync(IReadOnlyList<AddressablesPreloadRegistration> preload, CancellationToken cancellationToken)
        {
            HashSet<string> seenPreload = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < preload.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddressablesPreloadRegistration registration = preload[i];
                string preloadId = $"{registration.AssetType.FullName}|{registration.Key}";
                if (!seenPreload.Add(preloadId))
                {
                    continue;
                }

                IAssetHandle handle = await assetReferenceHandler.AcquireByTypeAsync(registration.AssetType, registration.Key, registration.Mode, true, cancellationToken);
                RememberPreloadedAsset(registration.AssetType, handle?.UntypedAsset);
            }
        }

        private void RememberPreloadedAsset(Type assetType, UnityEngine.Object asset)
        {
            if (assetType == null || asset == null)
            {
                return;
            }

            lock (initSync)
            {
                if (!preloadedAssetsByType.ContainsKey(assetType))
                {
                    preloadedAssetsByType.Add(assetType, asset);
                }
            }
        }

        private async Task<IReadOnlyList<IAssetHandle<T>>> LoadLabelHandlesAsync<T>(IReadOnlyList<string> keys, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            List<IAssetHandle<T>> handles = new List<IAssetHandle<T>>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                handles.Add(await assetReferenceHandler.AcquireAsync<T>(keys[i], cancellationToken));
            }

            return handles;
        }

        private IReadOnlyList<IAssetHandle<T>> LoadLabelHandlesSync<T>(IReadOnlyList<string> keys, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            List<IAssetHandle<T>> handles = new List<IAssetHandle<T>>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                AssetReference reference = new AssetReference(keys[i]);
                IAssetHandle<T> handle = Load<T>(reference, cancellationToken);
                handles.Add(handle);
            }

            return handles;
        }

        private async Task CompleteLoadAsync<T>(string key, AssetHandle<T> handle, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            try
            {
                IAssetHandle<T> loaded = await assetReferenceHandler.AcquireAsync<T>(key, cancellationToken);
                handle.Complete(loaded);
            }
            catch (Exception exception)
            {
                handle.Fail(exception);
            }
        }

        private async Task<IReadOnlyList<string>> ResolveLabelKeysAsync<T>(AssetLabelReference label, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            GuardLabel(label);
            return await client.ResolveLabelAsync(typeof(T), label, cancellationToken);
        }

        private string ResolveReferenceKey(AssetReference reference)
        {
            GuardReference(reference);
            string key = reference.RuntimeKey?.ToString();
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Asset reference is not valid.", nameof(reference));
            }

            return key;
        }

        private void GuardRuntimeInvariants()
        {
            if (client == null || assetReferenceHandler == null || assetPreloadHandler == null)
            {
                throw new InvalidOperationException("Addressables gateway is not properly initialized.");
            }
        }

        private static void GuardLabel(AssetLabelReference label)
        {
            if (label == null || string.IsNullOrWhiteSpace(label.labelString))
            {
                throw new ArgumentException("Label reference cannot be empty.", nameof(label));
            }
        }

        private static void GuardReference(AssetReference reference)
        {
            if (reference == null || reference.RuntimeKey == null)
            {
                throw new ArgumentException("Asset reference is not valid.", nameof(reference));
            }
        }
    }
}
