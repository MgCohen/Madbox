using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using UnityEngine.AddressableAssets;

namespace Madbox.Addressables
{
    public sealed class AddressablesGateway : IAddressablesGateway
    {
        public AddressablesGateway(IAddressablesAssetClient client, IAddressablesPreloadRegistry preloadRegistry)
            : this(client, CreatePreloadSource(preloadRegistry))
        {
        }

        private AddressablesGateway(IAddressablesAssetClient client, IAddressablesPreloadSource preloadSource)
        {
            GuardConstructor(client, preloadSource);
            this.client = client;
            leaseStore = new AddressablesLeaseStore(client);
            startupCoordinator = new AddressablesStartupCoordinator(client, preloadSource, leaseStore);
        }

        private readonly IAddressablesAssetClient client;
        private readonly AddressablesStartupCoordinator startupCoordinator;
        private readonly AddressablesLeaseStore leaseStore;
        private bool initialized;

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            GuardCancellation(cancellationToken);
            if (initialized) { return; }
            await startupCoordinator.RunInitializationAsync(cancellationToken);
            initialized = true;
        }

        public async Task<IAssetHandle<T>> LoadAsync<T>(AssetKey key, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            GuardCancellation(cancellationToken);
            AddressablesLoadToken token = CreateToken<T>(key);
            return await leaseStore.AcquireAsync<T>(token, cancellationToken);
        }

        public async Task<IAssetGroupHandle<T>> LoadAsync<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            GuardCancellation(cancellationToken);
            GuardLabel(label);
            IReadOnlyList<AssetKey> keys = await ResolveLabelKeysAsync<T>(label, cancellationToken);
            IReadOnlyList<IAssetHandle<T>> handles = await LoadCatalogHandlesAsync<T>(keys, cancellationToken);
            return CreateGroupHandle(label, handles, typeof(T));
        }

        public Task<IAssetHandle<T>> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            GuardCancellation(cancellationToken);
            GuardReference(reference);
            AssetKey key = CreateAssetKeyFromReference(reference);
            return LoadAsync<T>(key, cancellationToken);
        }

        public Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            GuardCancellation(cancellationToken);
            GuardReference(reference);
            return LoadAsync<T>((AssetReference)reference, cancellationToken);
        }

        private async Task<IReadOnlyList<IAssetHandle<T>>> LoadCatalogHandlesAsync<T>(IReadOnlyList<AssetKey> keys, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            List<IAssetHandle<T>> handles = new List<IAssetHandle<T>>(keys.Count);
            foreach (AssetKey key in keys) { handles.Add(await LoadAsync<T>(key, cancellationToken)); }
            return handles;
        }

        private async Task<IReadOnlyList<AssetKey>> ResolveLabelKeysAsync<T>(AssetLabelReference label, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            return await client.ResolveLabelAsync(typeof(T), label, cancellationToken);
        }

        private IAssetGroupHandle<T> CreateGroupHandle<T>(AssetLabelReference label, IReadOnlyList<IAssetHandle<T>> handles, Type assetType) where T : UnityEngine.Object
        {
            string id = CreateGroupId(assetType, label);
            return new AssetGroupHandle<T>(id, handles);
        }

        private string CreateGroupId(Type assetType, AssetLabelReference label)
        {
            return $"{assetType.FullName}|label:{label.labelString}";
        }

        private AddressablesLoadToken CreateToken<T>(AssetKey key) where T : UnityEngine.Object
        {
            return new AddressablesLoadToken(typeof(T), key);
        }

        private AssetKey CreateAssetKeyFromReference(AssetReference reference)
        {
            object runtimeKey = reference.RuntimeKey;
            string keyValue = runtimeKey?.ToString();
            return new AssetKey(keyValue);
        }

        private void GuardConstructor(IAddressablesAssetClient client, IAddressablesPreloadSource preloadSource)
        {
            if (client == null) { throw new ArgumentNullException(nameof(client)); }
            if (preloadSource == null) { throw new ArgumentNullException(nameof(preloadSource)); }
        }

        private void GuardCancellation(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        private void GuardLabel(AssetLabelReference label)
        {
            if (label == null || string.IsNullOrWhiteSpace(label.labelString)) { throw new ArgumentException("Label reference cannot be empty.", nameof(label)); }
        }

        private void GuardReference(AssetReference reference)
        {
            if (reference == null) { throw new ArgumentException("Asset reference is not valid.", nameof(reference)); }
            object runtimeKey = reference.RuntimeKey;
            if (runtimeKey == null) { throw new ArgumentException("Asset reference is not valid.", nameof(reference)); }
            string keyValue = runtimeKey.ToString();
            if (string.IsNullOrWhiteSpace(keyValue)) { throw new ArgumentException("Asset reference is not valid.", nameof(reference)); }
        }

        private static IAddressablesPreloadSource CreatePreloadSource(IAddressablesPreloadRegistry preloadRegistry)
        {
            if (preloadRegistry == null) { throw new ArgumentNullException(nameof(preloadRegistry)); }
            if (preloadRegistry is IAddressablesPreloadSource source) { return source; }
            throw new ArgumentException("Preload registry must implement internal preload source contract.", nameof(preloadRegistry));
        }
    }
}
