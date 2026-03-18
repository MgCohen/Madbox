using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
#pragma warning disable SCA0003
#pragma warning disable SCA0006

namespace Madbox.Addressables
{
    internal sealed class AddressablesStartupCoordinator
    {
        public AddressablesStartupCoordinator(IAddressablesAssetClient client, AddressablesLeaseStore leaseStore, AddressablesPreloadRequestProvider requestProvider)
        {
            GuardConstructor(client, leaseStore, requestProvider);
            this.client = client;
            this.leaseStore = leaseStore;
            this.requestProvider = requestProvider;
        }

        private readonly IAddressablesAssetClient client;
        private readonly AddressablesLeaseStore leaseStore;
        private readonly AddressablesPreloadRequestProvider requestProvider;

        internal async Task RunInitializationAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await TrySyncCatalogAndContentAsync(cancellationToken);
            IReadOnlyList<AddressablesPreloadRequest> requests = await requestProvider.LoadRequestsAsync(cancellationToken, "Continuing without startup preload.");
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

        private void GuardConstructor(IAddressablesAssetClient client, AddressablesLeaseStore leaseStore, AddressablesPreloadRequestProvider requestProvider)
        {
            if (client == null) { throw new ArgumentNullException(nameof(client)); }
            if (leaseStore == null) { throw new ArgumentNullException(nameof(leaseStore)); }
            if (requestProvider == null) { throw new ArgumentNullException(nameof(requestProvider)); }
        }
    }
}
#pragma warning restore SCA0006
#pragma warning restore SCA0003
