using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using UnityEngine.AddressableAssets;

namespace Madbox.Addressables
{
    internal sealed class AddressablesStartupCoordinator
    {
        private const string preloadConfigLabel = "addressables-preload-config";

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
            AssetLabelReference label = CreatePreloadLabel();
            IReadOnlyList<AssetKey> keys = await client.ResolveLabelAsync(typeof(AddressablesPreloadConfigWrapper), label, cancellationToken);
            return await BuildRequestsFromWrappersAsync(keys, cancellationToken);
        }

        private AssetLabelReference CreatePreloadLabel()
        {
            return new AssetLabelReference { labelString = preloadConfigLabel };
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

        private void GuardConstructor(IAddressablesAssetClient client, AddressablesLeaseStore leaseStore, AddressablesPreloadConfigRequestBuilder requestBuilder)
        {
            if (client == null) { throw new ArgumentNullException(nameof(client)); }
            if (leaseStore == null) { throw new ArgumentNullException(nameof(leaseStore)); }
            if (requestBuilder == null) { throw new ArgumentNullException(nameof(requestBuilder)); }
        }
    }
}
