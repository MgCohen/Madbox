using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.Scope.Contracts;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;
#pragma warning disable SCA0006

namespace Madbox.Addressables
{
    public sealed class AddressablesGateway : IAddressablesGateway, IAsyncLayerInitializable
    {
        [Inject]
        public AddressablesGateway() : this(new AddressablesAssetClient())
        {
        }

        internal AddressablesGateway(IAddressablesAssetClient client)
        {
            if (client == null) { throw new ArgumentNullException(nameof(client)); }
            this.client = client;
            leaseStore = new AddressablesLeaseStore(client);
        }

        private readonly IAddressablesAssetClient client;
        private readonly AddressablesLeaseStore leaseStore;
        private bool initialized;
        private readonly object initSync = new object();

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            GuardRuntimeInvariants();
            return InitializeCoreAsync(null, cancellationToken);
        }

        Task IAsyncLayerInitializable.InitializeAsync(ILayerInitializationContext context, IObjectResolver resolver, CancellationToken cancellationToken)
        {
            return InitializeCoreAsync(context, cancellationToken);
        }

        public async Task<IAssetHandle<T>> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            GuardRuntimeInvariants();
            cancellationToken.ThrowIfCancellationRequested();
            string key = ResolveReferenceKey(reference);
            return await leaseStore.AcquireAsync<T>(key, cancellationToken);
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
            Type assetType = typeof(T);
            string groupId = CreateGroupId(assetType, label);
            return new AssetGroupHandle<T>(groupId, handles);
        }

        public IAssetHandle<T> Load<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            GuardRuntimeInvariants();
            cancellationToken.ThrowIfCancellationRequested();
            string key = ResolveReferenceKey(reference);
            string id = CreateAssetId(typeof(T), key);
            AssetHandle<T> handle = new AssetHandle<T>(id);
            _ = CompleteLoadAsync(reference, handle, cancellationToken);
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
            Type assetType = typeof(T);
            string groupId = CreateGroupId(assetType, label);
            return new AssetGroupHandle<T>(groupId, handles);
        }

        private async Task InitializeCoreAsync(ILayerInitializationContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsAlreadyInitialized()) { return; }
            await RunCatalogSyncAsync(cancellationToken);
            IReadOnlyList<PreloadRegistration> preload = await LoadPreloadRegistrationsAsync(cancellationToken);
            await ApplyPreloadAsync(preload, context, cancellationToken);
            MarkInitialized();
        }

        private bool IsAlreadyInitialized()
        {
            lock (initSync)
            {
                return initialized;
            }
        }

        private void MarkInitialized()
        {
            lock (initSync)
            {
                initialized = true;
            }
        }

        private async Task RunCatalogSyncAsync(CancellationToken cancellationToken)
        {
            try { await client.SyncCatalogAndContentAsync(cancellationToken); }
            catch (Exception exception) when (exception is not OperationCanceledException) { UnityEngine.Debug.LogWarning($"Addressables catalog/content sync failed. Continuing startup. {exception.GetType().Name}: {exception.Message}"); }
        }

        private async Task<IReadOnlyList<PreloadRegistration>> LoadPreloadRegistrationsAsync(CancellationToken cancellationToken)
        {
            AddressablesPreloadConfig config;
            try
            {
                UnityEngine.Object raw = await client.LoadAssetAsync(AddressablesPreloadConstants.BootstrapConfigAssetKey, typeof(AddressablesPreloadConfig), cancellationToken);
                config = raw as AddressablesPreloadConfig;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                UnityEngine.Debug.LogWarning($"Addressables preload config '{AddressablesPreloadConstants.BootstrapConfigAssetKey}' was not found or failed to load. Continuing without startup preload. {exception.GetType().Name}: {exception.Message}");
                return Array.Empty<PreloadRegistration>();
            }

            if (config == null) { return Array.Empty<PreloadRegistration>(); }
            return BuildPreloadRegistrations(config);
        }

        private IReadOnlyList<PreloadRegistration> BuildPreloadRegistrations(AddressablesPreloadConfig config)
        {
            List<PreloadRegistration> registrations = new List<PreloadRegistration>();
            IReadOnlyList<AddressablesPreloadConfigEntry> entries = config.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                IReadOnlyList<PreloadRegistration> entryRegistrations = BuildRegistrationsForEntry(entries[i], i);
                registrations.AddRange(entryRegistrations);
            }
            return registrations;
        }

        private IReadOnlyList<PreloadRegistration> BuildRegistrationsForEntry(AddressablesPreloadConfigEntry entry, int index)
        {
            GuardPreloadEntry(entry, index);
            Type assetType = entry.AssetType.Type;
            if (entry.ReferenceType == PreloadReferenceType.AssetReference)
            {
                string key = ResolveReferenceKey(entry.AssetReference);
                return new[] { new PreloadRegistration(assetType, key, entry.Mode) };
            }

            if (entry.ReferenceType == PreloadReferenceType.LabelReference)
            {
                IReadOnlyList<string> keys = client.ResolveLabelAsync(assetType, entry.LabelReference, CancellationToken.None).GetAwaiter().GetResult();
                List<PreloadRegistration> byLabel = new List<PreloadRegistration>(keys.Count);
                for (int i = 0; i < keys.Count; i++)
                {
                    PreloadRegistration registration = new PreloadRegistration(assetType, keys[i], entry.Mode);
                    byLabel.Add(registration);
                }
                return byLabel;
            }

            throw new InvalidOperationException($"Invalid preload config entry at index {index}. Unsupported reference type '{entry.ReferenceType}'.");
        }

        private async Task ApplyPreloadAsync(IReadOnlyList<PreloadRegistration> preload, ILayerInitializationContext context, CancellationToken cancellationToken)
        {
            Dictionary<Type, IAssetHandle> byType = context == null ? null : new Dictionary<Type, IAssetHandle>();
            HashSet<string> seenPreload = context == null ? new HashSet<string>(StringComparer.Ordinal) : null;
            for (int i = 0; i < preload.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                PreloadRegistration registration = preload[i];
                if (context == null)
                {
                    string preloadId = CreateAssetId(registration.AssetType, registration.Key);
                    if (!seenPreload.Add(preloadId)) { continue; }
                    await leaseStore.AcquireByTypeAsync(registration.AssetType, registration.Key, registration.Mode, true, cancellationToken);
                    continue;
                }

                IAssetHandle handle = await leaseStore.AcquireByTypeAsync(registration.AssetType, registration.Key, registration.Mode, true, cancellationToken);
                RegisterForChildScope(byType, registration.AssetType, handle, context);
            }
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

        private async Task<IReadOnlyList<IAssetHandle<T>>> LoadLabelHandlesAsync<T>(IReadOnlyList<string> keys, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            List<IAssetHandle<T>> handles = new List<IAssetHandle<T>>(keys.Count);
            for (int i = 0; i < keys.Count; i++) { handles.Add(await leaseStore.AcquireAsync<T>(keys[i], cancellationToken)); }
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

        private void GuardRuntimeInvariants()
        {
            if (client == null) { throw new InvalidOperationException("Addressables gateway is not properly initialized."); }
            if (leaseStore == null) { throw new InvalidOperationException("Addressables gateway is not properly initialized."); }
        }

        private async Task CompleteLoadAsync<T>(AssetReference reference, AssetHandle<T> handle, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            try
            {
                IAssetHandle<T> loaded = await LoadAsync<T>(reference, cancellationToken);
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

        private string CreateGroupId(Type assetType, AssetLabelReference label)
        {
            return $"{assetType.FullName}|label:{label.labelString}";
        }

        private string CreateAssetId(Type assetType, string key)
        {
            return $"{assetType.FullName}|{key}";
        }

        private string ResolveReferenceKey(AssetReference reference)
        {
            GuardReference(reference);
            string key = reference.RuntimeKey?.ToString();
            if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentException("Asset reference is not valid.", nameof(reference)); }
            return key;
        }

        private void GuardPreloadEntry(AddressablesPreloadConfigEntry entry, int index)
        {
            if (entry == null) { throw new InvalidOperationException($"Invalid preload config entry at index {index}. Entry is null."); }
            Type assetType = entry.AssetType?.Type;
            if (assetType == null) { throw new InvalidOperationException($"Invalid preload config entry at index {index}. AssetType is missing or unresolved."); }
            if (!typeof(UnityEngine.Object).IsAssignableFrom(assetType)) { throw new InvalidOperationException($"Invalid preload config entry at index {index}. AssetType '{assetType.FullName}' must inherit UnityEngine.Object."); }
            if (entry.ReferenceType == PreloadReferenceType.AssetReference && entry.AssetReference == null) { throw new InvalidOperationException($"Invalid preload config entry at index {index}. AssetReference is missing."); }
            if (entry.ReferenceType == PreloadReferenceType.LabelReference && (entry.LabelReference == null || string.IsNullOrWhiteSpace(entry.LabelReference.labelString))) { throw new InvalidOperationException($"Invalid preload config entry at index {index}. LabelReference is missing labelString."); }
        }

        private void GuardLabel(AssetLabelReference label)
        {
            if (label == null || string.IsNullOrWhiteSpace(label.labelString)) { throw new ArgumentException("Label reference cannot be empty.", nameof(label)); }
        }

        private void GuardReference(AssetReference reference)
        {
            if (reference == null) { throw new ArgumentException("Asset reference is not valid.", nameof(reference)); }
            if (reference.RuntimeKey == null) { throw new ArgumentException("Asset reference is not valid.", nameof(reference)); }
        }

        private readonly struct PreloadRegistration
        {
            public PreloadRegistration(Type assetType, string key, PreloadMode mode)
            {
                if (assetType == null) { throw new ArgumentNullException(nameof(assetType)); }
                if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentException("Preload key cannot be empty.", nameof(key)); }
                AssetType = assetType;
                Key = key;
                Mode = mode;
            }

            public Type AssetType { get; }
            public string Key { get; }
            public PreloadMode Mode { get; }
        }
    }
}
#pragma warning restore SCA0006
