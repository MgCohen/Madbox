using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.Scope.Contracts;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;

namespace Madbox.Addressables
{
    public sealed class AddressablesGateway : IAddressablesGateway, IAsyncLayerInitializable
    {
        public AddressablesGateway(IAddressablesAssetClient client, IAssetReferenceHandler assetReferenceHandler, IAssetPreloadHandler assetPreloadHandler)
        {
            if (client == null)
{
    throw new ArgumentNullException(nameof(client));
}
            if (assetReferenceHandler == null)
{
    throw new ArgumentNullException(nameof(assetReferenceHandler));
}
            if (assetPreloadHandler == null)
{
    throw new ArgumentNullException(nameof(assetPreloadHandler));
}
            this.client = client;
            this.assetReferenceHandler = assetReferenceHandler;
            this.assetPreloadHandler = assetPreloadHandler;
        }

        private readonly IAddressablesAssetClient client;
        private readonly IAssetReferenceHandler assetReferenceHandler;
        private readonly IAssetPreloadHandler assetPreloadHandler;
        private bool initialized;
        private readonly object initSync = new object();

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (client == null || assetReferenceHandler == null || assetPreloadHandler == null) throw new InvalidOperationException("Addressables gateway is not properly initialized.");
            cancellationToken.ThrowIfCancellationRequested();
            lock (initSync) { if (initialized) return; }
            await InitializePreloadAsync(null, cancellationToken);
            lock (initSync) { initialized = true; }
        }

        async Task IAsyncLayerInitializable.InitializeAsync(ILayerInitializationContext context, IObjectResolver resolver, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (initSync) { if (initialized) return; }
            await InitializePreloadAsync(context, cancellationToken);
            lock (initSync) { initialized = true; }
        }

        private async Task InitializePreloadAsync(ILayerInitializationContext context, CancellationToken cancellationToken)
        {
            try { await client.SyncCatalogAndContentAsync(cancellationToken); } catch (Exception exception) when (exception is not OperationCanceledException) { UnityEngine.Debug.LogWarning($"Addressables catalog/content sync failed. Continuing startup. {exception.GetType().Name}: {exception.Message}"); }
            IReadOnlyList<AddressablesPreloadRegistration> preload;
            try { UnityEngine.Object raw = await client.LoadAssetAsync(AddressablesPreloadConstants.BootstrapConfigAssetKey, typeof(AddressablesPreloadConfig), cancellationToken); AddressablesPreloadConfig config = raw as AddressablesPreloadConfig; preload = config == null ? Array.Empty<AddressablesPreloadRegistration>() : await assetPreloadHandler.BuildAsync(config, cancellationToken); } catch (Exception exception) when (exception is not OperationCanceledException) { UnityEngine.Debug.LogWarning($"Addressables preload config '{AddressablesPreloadConstants.BootstrapConfigAssetKey}' was not found or failed to load. Continuing without startup preload. {exception.GetType().Name}: {exception.Message}"); preload = Array.Empty<AddressablesPreloadRegistration>(); }
            await ApplyPreloadAsync(preload, context, cancellationToken);
        }

        private async Task ApplyPreloadAsync(IReadOnlyList<AddressablesPreloadRegistration> preload, ILayerInitializationContext context, CancellationToken cancellationToken)
        {
            Dictionary<Type, IAssetHandle> byType = context == null ? null : new Dictionary<Type, IAssetHandle>();
            HashSet<string> seenPreload = context == null ? new HashSet<string>(StringComparer.Ordinal) : null;
            for (int i = 0; i < preload.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddressablesPreloadRegistration registration = preload[i];
                await ApplyPreloadRegistrationAsync(registration, context, byType, seenPreload, cancellationToken);
            }
        }

        private async Task ApplyPreloadRegistrationAsync(AddressablesPreloadRegistration registration, ILayerInitializationContext context, IDictionary<Type, IAssetHandle> byType, ISet<string> seenPreload, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                string preloadId = $"{registration.AssetType.FullName}|{registration.Key}";
                if (!seenPreload.Add(preloadId)) return;
                await assetReferenceHandler.AcquireByTypeAsync(registration.AssetType, registration.Key, registration.Mode, true, cancellationToken);
                return;
            }

            IAssetHandle handle = await assetReferenceHandler.AcquireByTypeAsync(registration.AssetType, registration.Key, registration.Mode, true, cancellationToken);
            RegisterForChildScope(byType, registration.AssetType, handle, context);
        }

        private void RegisterForChildScope(IDictionary<Type, IAssetHandle> byType, Type assetType, IAssetHandle handle, ILayerInitializationContext context)
        {
            if (byType.ContainsKey(assetType))
            {
                handle.Release();
                throw new InvalidOperationException($"Duplicate preload registration for service type '{assetType.FullName}'. Use one preload entry per service type.");
            }

            byType[assetType] = handle;
            context.RegisterInstanceForChild(assetType, handle.UntypedAsset, Lifetime.Singleton, ChildScopeDelegationPolicy.AllDescendants);
        }

        public async Task<IAssetHandle<T>> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            if (client == null || assetReferenceHandler == null || assetPreloadHandler == null) throw new InvalidOperationException("Addressables gateway is not properly initialized.");
            cancellationToken.ThrowIfCancellationRequested();
            if (reference == null || reference.RuntimeKey == null) throw new ArgumentException("Asset reference is not valid.", nameof(reference));
            string key = reference.RuntimeKey.ToString();
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Asset reference is not valid.", nameof(reference));
            return await assetReferenceHandler.AcquireAsync<T>(key, cancellationToken);
        }

        public Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            if (client == null || assetReferenceHandler == null || assetPreloadHandler == null) throw new InvalidOperationException("Addressables gateway is not properly initialized.");
            cancellationToken.ThrowIfCancellationRequested();
            if (reference == null || reference.RuntimeKey == null) throw new ArgumentException("Asset reference is not valid.", nameof(reference));
            string key = reference.RuntimeKey.ToString();
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Asset reference is not valid.", nameof(reference));
            return assetReferenceHandler.AcquireAsync<T>(key, cancellationToken);
        }

        public async Task<IAssetGroupHandle<T>> LoadAsync<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            if (client == null || assetReferenceHandler == null || assetPreloadHandler == null) throw new InvalidOperationException("Addressables gateway is not properly initialized.");
            cancellationToken.ThrowIfCancellationRequested();
            if (label == null || string.IsNullOrWhiteSpace(label.labelString)) throw new ArgumentException("Label reference cannot be empty.", nameof(label));
            IReadOnlyList<string> keys = await client.ResolveLabelAsync(typeof(T), label, cancellationToken);
            List<IAssetHandle<T>> handles = new List<IAssetHandle<T>>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                handles.Add(await assetReferenceHandler.AcquireAsync<T>(keys[i], cancellationToken));
            }
            return new AssetGroupHandle<T>(handles);
        }

        public IAssetHandle<T> Load<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            if (client == null || assetReferenceHandler == null || assetPreloadHandler == null) throw new InvalidOperationException("Addressables gateway is not properly initialized.");
            cancellationToken.ThrowIfCancellationRequested();
            if (reference == null || reference.RuntimeKey == null) throw new ArgumentException("Asset reference is not valid.", nameof(reference));
            string key = reference.RuntimeKey.ToString();
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Asset reference is not valid.", nameof(reference));
            return StartLoadByKey<T>(key, cancellationToken);
        }

        public IAssetHandle<T> Load<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            if (client == null || assetReferenceHandler == null || assetPreloadHandler == null) throw new InvalidOperationException("Addressables gateway is not properly initialized.");
            cancellationToken.ThrowIfCancellationRequested();
            if (reference == null || reference.RuntimeKey == null) throw new ArgumentException("Asset reference is not valid.", nameof(reference));
            string key = reference.RuntimeKey.ToString();
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Asset reference is not valid.", nameof(reference));
            return StartLoadByKey<T>(key, cancellationToken);
        }

        public IAssetGroupHandle<T> Load<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            if (client == null || assetReferenceHandler == null || assetPreloadHandler == null) throw new InvalidOperationException("Addressables gateway is not properly initialized.");
            cancellationToken.ThrowIfCancellationRequested();
            if (label == null || string.IsNullOrWhiteSpace(label.labelString)) throw new ArgumentException("Label reference cannot be empty.", nameof(label));
            IReadOnlyList<string> keys = client.ResolveLabelAsync(typeof(T), label, cancellationToken).GetAwaiter().GetResult();
            IReadOnlyList<IAssetHandle<T>> handles = LoadLabelHandlesSync<T>(keys, cancellationToken);
            return new AssetGroupHandle<T>(handles);
        }

        private IReadOnlyList<IAssetHandle<T>> LoadLabelHandlesSync<T>(IReadOnlyList<string> keys, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            List<IAssetHandle<T>> handles = new List<IAssetHandle<T>>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                IAssetHandle<T> handle = StartLoadByKey<T>(keys[i], cancellationToken);
                handles.Add(handle);
            }
            return handles;
        }

        private IAssetHandle<T> StartLoadByKey<T>(string key, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            AssetHandle<T> handle = new AssetHandle<T>();
            _ = CompleteLoadByKeyAsync(key, handle, cancellationToken);
            return handle;
        }

        private async Task CompleteLoadByKeyAsync<T>(string key, AssetHandle<T> handle, CancellationToken cancellationToken) where T : UnityEngine.Object
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

    }
}


