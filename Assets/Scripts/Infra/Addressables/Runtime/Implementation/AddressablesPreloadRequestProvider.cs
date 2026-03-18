using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;

namespace Madbox.Addressables
{
    internal sealed class AddressablesPreloadRequestProvider
    {
        public AddressablesPreloadRequestProvider(IAddressablesAssetClient client, AddressablesPreloadConfigRequestBuilder requestBuilder)
        {
            if (client == null) { throw new ArgumentNullException(nameof(client)); }
            if (requestBuilder == null) { throw new ArgumentNullException(nameof(requestBuilder)); }
            this.client = client;
            this.requestBuilder = requestBuilder;
        }

        private readonly IAddressablesAssetClient client;
        private readonly AddressablesPreloadConfigRequestBuilder requestBuilder;

        public async Task<IReadOnlyList<AddressablesPreloadRequest>> LoadRequestsAsync(CancellationToken cancellationToken, string missingConfigWarning)
        {
            GuardMissingConfigWarning(missingConfigWarning);
            AssetKey configKey = new AssetKey(AddressablesPreloadConstants.BootstrapConfigAssetKey);
            AddressablesPreloadConfig config = await TryLoadConfigAsync(configKey, cancellationToken, missingConfigWarning);
            return BuildRequests(config, configKey);
        }

        private IReadOnlyList<AddressablesPreloadRequest> BuildRequests(AddressablesPreloadConfig config, AssetKey configKey)
        {
            if (config == null) { return Array.Empty<AddressablesPreloadRequest>(); }
            return BuildAndRelease(config, configKey);
        }

        private IReadOnlyList<AddressablesPreloadRequest> BuildAndRelease(AddressablesPreloadConfig config, AssetKey configKey)
        {
            try
            {
                return BuildRequestsCore(config, configKey);
            }
            finally
            {
                client.Release(config);
            }
        }

        private IReadOnlyList<AddressablesPreloadRequest> BuildRequestsCore(AddressablesPreloadConfig config, AssetKey configKey)
        {
            List<AddressablesPreloadRequest> requests = new List<AddressablesPreloadRequest>();
            requestBuilder.AppendRequests(config, configKey, requests);
            return requests;
        }

        private async Task<AddressablesPreloadConfig> TryLoadConfigAsync(AssetKey configKey, CancellationToken cancellationToken, string missingConfigWarning)
        {
            try { return await client.LoadAssetAsync<AddressablesPreloadConfig>(configKey, cancellationToken); }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                UnityEngine.Debug.LogWarning($"Addressables preload config '{configKey.Value}' was not found or failed to load. {missingConfigWarning} {exception.GetType().Name}: {exception.Message}");
                return null;
            }
        }

        private void GuardMissingConfigWarning(string missingConfigWarning)
        {
            if (string.IsNullOrWhiteSpace(missingConfigWarning)) { throw new ArgumentException("Warning context cannot be empty.", nameof(missingConfigWarning)); }
        }
    }
}
