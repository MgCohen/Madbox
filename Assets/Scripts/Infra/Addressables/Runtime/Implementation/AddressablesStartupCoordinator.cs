using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using UnityEngine.AddressableAssets;
#pragma warning disable SCA0003
#pragma warning disable SCA0006

namespace Madbox.Addressables
{
    internal sealed class AddressablesStartupCoordinator
    {
        public AddressablesStartupCoordinator(IAddressablesAssetClient client, AddressablesLeaseStore leaseStore, AddressablesPreloadConfigRequestBuilder requestBuilder)
        {
            GuardConstructor(client, leaseStore, requestBuilder);
            this.client = client;
            this.leaseStore = leaseStore;
            this.requestBuilder = requestBuilder;
        }

        private readonly IAddressablesAssetClient client;
        private readonly AddressablesLeaseStore leaseStore;
        private readonly AddressablesPreloadConfigRequestBuilder requestBuilder;

        internal async Task RunInitializationAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await TrySyncCatalogAndContentAsync(cancellationToken);
            IReadOnlyList<AddressablesPreloadRequest> requests = await LoadPreloadRequestsAsync(cancellationToken);
            await ApplyPreloadRequestsAsync(requests, cancellationToken);
        }

        private async Task TrySyncCatalogAndContentAsync(CancellationToken cancellationToken)
        {
            try { await client.SyncCatalogAndContentAsync(cancellationToken); }
            catch (Exception exception) when (exception is not OperationCanceledException) { UnityEngine.Debug.LogWarning($"Addressables catalog/content sync failed. Continuing startup. {exception.GetType().Name}: {exception.Message}"); }
        }

        private async Task ApplyPreloadRequestsAsync(IReadOnlyList<AddressablesPreloadRequest> requests, CancellationToken cancellationToken)
        {
            foreach (AddressablesPreloadRequest request in requests)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ApplyPreloadRequestAsync(request, cancellationToken);
            }
        }

        private async Task ApplyPreloadRequestAsync(AddressablesPreloadRequest request, CancellationToken cancellationToken)
        {
            if (request.IsCatalog) { await ApplyCatalogPreloadRequestAsync(request, cancellationToken); return; }
            await leaseStore.PreloadByTypeAsync(request.AssetType, request.Key, request.Mode, cancellationToken);
        }

        private async Task ApplyCatalogPreloadRequestAsync(AddressablesPreloadRequest request, CancellationToken cancellationToken)
        {
            IReadOnlyList<AssetKey> keys = await client.ResolveLabelAsync(request.AssetType, request.Label, cancellationToken);
            foreach (AssetKey key in keys) { await leaseStore.PreloadByTypeAsync(request.AssetType, key, request.Mode, cancellationToken); }
        }

        private async Task<IReadOnlyList<AddressablesPreloadRequest>> LoadPreloadRequestsAsync(CancellationToken cancellationToken)
        {
            AssetKey bootstrapConfigKey = new AssetKey(AddressablesPreloadConstants.BootstrapConfigAssetKey);
            AddressablesPreloadBootstrapConfig bootstrapConfig = await TryLoadBootstrapConfigAsync(bootstrapConfigKey, cancellationToken);
            if (bootstrapConfig == null) { return Array.Empty<AddressablesPreloadRequest>(); }
            try
            {
                IReadOnlyList<AssetKey> keys = ResolveWrapperKeys(bootstrapConfig, bootstrapConfigKey);
                return await BuildRequestsFromWrappersAsync(keys, cancellationToken);
            }
            finally
            {
                client.Release(bootstrapConfig);
            }
        }

        private async Task<AddressablesPreloadBootstrapConfig> TryLoadBootstrapConfigAsync(AssetKey configKey, CancellationToken cancellationToken)
        {
            try { return await client.LoadAssetAsync<AddressablesPreloadBootstrapConfig>(configKey, cancellationToken); }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                UnityEngine.Debug.LogWarning($"Addressables preload bootstrap config '{configKey.Value}' was not found or failed to load. Continuing without startup preload. {exception.GetType().Name}: {exception.Message}");
                return null;
            }
        }

        private async Task<IReadOnlyList<AddressablesPreloadRequest>> BuildRequestsFromWrappersAsync(IReadOnlyList<AssetKey> keys, CancellationToken cancellationToken)
        {
            List<AddressablesPreloadRequest> requests = new List<AddressablesPreloadRequest>();
            foreach (AssetKey key in keys) { await LoadWrapperRequestsAsync(key, requests, cancellationToken); }
            return requests;
        }

        private async Task LoadWrapperRequestsAsync(AssetKey key, ICollection<AddressablesPreloadRequest> requests, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddressablesPreloadConfigWrapper wrapper = await client.LoadAssetAsync<AddressablesPreloadConfigWrapper>(key, cancellationToken);
            try { requestBuilder.AppendRequests(wrapper, key, requests); }
            finally { client.Release(wrapper); }
        }

        private IReadOnlyList<AssetKey> ResolveWrapperKeys(AddressablesPreloadBootstrapConfig config, AssetKey configKey)
        {
            if (config == null) { throw new InvalidOperationException($"Preload bootstrap config '{configKey.Value}' failed to load."); }
            IReadOnlyList<AssetReferenceT<AddressablesPreloadConfigWrapper>> references = config.Wrappers;
            List<AssetKey> keys = new List<AssetKey>(references.Count);
            for (int i = 0; i < references.Count; i++)
            {
                AssetReferenceT<AddressablesPreloadConfigWrapper> reference = references[i];
                string keyValue = reference?.RuntimeKey?.ToString();
                if (string.IsNullOrWhiteSpace(keyValue))
                {
                    throw new InvalidOperationException($"Invalid preload bootstrap config '{configKey.Value}' at wrapper index {i}. Wrapper reference has no runtime key.");
                }

                keys.Add(new AssetKey(keyValue));
            }

            return keys;
        }

        private void GuardConstructor(IAddressablesAssetClient client, AddressablesLeaseStore leaseStore, AddressablesPreloadConfigRequestBuilder requestBuilder)
        {
            if (client == null) { throw new ArgumentNullException(nameof(client)); }
            if (leaseStore == null) { throw new ArgumentNullException(nameof(leaseStore)); }
            if (requestBuilder == null) { throw new ArgumentNullException(nameof(requestBuilder)); }
        }
    }
}
#pragma warning restore SCA0006
#pragma warning restore SCA0003
