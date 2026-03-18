using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.Scope.Contracts;
using UnityEngine.AddressableAssets;
using VContainer;
#pragma warning disable SCA0006

namespace Madbox.Addressables
{
    public sealed class AddressablesLayerInitializer : IAsyncLayerInitializable
    {
        public AddressablesLayerInitializer(IAddressablesGateway gateway, IAddressablesAssetClient client)
        {
            GuardConstructor(gateway, client);
            this.gateway = gateway;
            this.client = client;
            requestBuilder = new AddressablesPreloadConfigRequestBuilder();
        }

        private readonly IAddressablesGateway gateway;
        private readonly IAddressablesAssetClient client;
        private readonly AddressablesPreloadConfigRequestBuilder requestBuilder;
        private readonly List<IAssetHandle> startupHandles = new List<IAssetHandle>();
        private static readonly MethodInfo loadByKeyMethod = CreateLoadByKeyMethod();

        public Task InitializeAsync(ILayerInitializationContext context, IObjectResolver resolver, CancellationToken cancellationToken)
        {
            GuardCancellation(cancellationToken);
            return InitializeAndRegisterAsync(context, cancellationToken);
        }

        private async Task InitializeAndRegisterAsync(ILayerInitializationContext context, CancellationToken cancellationToken)
        {
            await gateway.InitializeAsync(cancellationToken);
            IReadOnlyList<AddressablesPreloadRequest> requests = await LoadPreloadRequestsAsync(cancellationToken);
            await RegisterPreloadedAssetsAsync(requests, context, cancellationToken);
        }

        private async Task<IReadOnlyList<AddressablesPreloadRequest>> LoadPreloadRequestsAsync(CancellationToken cancellationToken)
        {
            AssetKey configKey = new AssetKey(AddressablesPreloadConstants.BootstrapConfigAssetKey);
            AddressablesPreloadBootstrapConfig config = await TryLoadBootstrapConfigAsync(configKey, cancellationToken);
            if (config == null) { return Array.Empty<AddressablesPreloadRequest>(); }
            try
            {
                List<AddressablesPreloadRequest> requests = new List<AddressablesPreloadRequest>();
                IReadOnlyList<AssetReferenceT<AddressablesPreloadConfigWrapper>> wrappers = config.Wrappers;
                for (int i = 0; i < wrappers.Count; i++)
                {
                    string keyValue = wrappers[i]?.RuntimeKey?.ToString();
                    if (string.IsNullOrWhiteSpace(keyValue))
                    {
                        throw new InvalidOperationException($"Invalid preload bootstrap config '{configKey.Value}' at wrapper index {i}. Wrapper reference has no runtime key.");
                    }

                    AssetKey wrapperKey = new AssetKey(keyValue);
                    AddressablesPreloadConfigWrapper wrapper = await client.LoadAssetAsync<AddressablesPreloadConfigWrapper>(wrapperKey, cancellationToken);
                    try { requestBuilder.AppendRequests(wrapper, wrapperKey, requests); }
                    finally { client.Release(wrapper); }
                }

                return requests;
            }
            finally
            {
                client.Release(config);
            }
        }

        private async Task<AddressablesPreloadBootstrapConfig> TryLoadBootstrapConfigAsync(AssetKey configKey, CancellationToken cancellationToken)
        {
            try { return await client.LoadAssetAsync<AddressablesPreloadBootstrapConfig>(configKey, cancellationToken); }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                UnityEngine.Debug.LogWarning($"Addressables preload bootstrap config '{configKey.Value}' was not found or failed to load. Skipping child preload injection. {exception.GetType().Name}: {exception.Message}");
                return null;
            }
        }

        private async Task RegisterPreloadedAssetsAsync(IReadOnlyList<AddressablesPreloadRequest> requests, ILayerInitializationContext context, CancellationToken cancellationToken)
        {
            Dictionary<Type, IAssetHandle> byType = new Dictionary<Type, IAssetHandle>();
            for (int i = 0; i < requests.Count; i++)
            {
                AddressablesPreloadRequest request = requests[i];
                if (request.IsCatalog)
                {
                    IReadOnlyList<AssetKey> keys = await client.ResolveLabelAsync(request.AssetType, request.Label, cancellationToken);
                    for (int k = 0; k < keys.Count; k++) { await RegisterSingleRequestAsync(request.AssetType, keys[k], byType, context, cancellationToken); }
                    continue;
                }

                await RegisterSingleRequestAsync(request.AssetType, request.Key, byType, context, cancellationToken);
            }
        }

        private async Task RegisterSingleRequestAsync(Type assetType, AssetKey key, IDictionary<Type, IAssetHandle> byType, ILayerInitializationContext context, CancellationToken cancellationToken)
        {
            IAssetHandle handle = await LoadHandleByTypeAsync(assetType, key, cancellationToken);
            if (byType.ContainsKey(assetType))
            {
                handle.Release();
                throw new InvalidOperationException($"Duplicate preload registration for service type '{assetType.FullName}'. Use one preload entry per service type.");
            }

            byType[assetType] = handle;
            startupHandles.Add(handle);
            context.RegisterInstanceForChild(assetType, handle.UntypedAsset, Lifetime.Singleton, ChildScopeDelegationPolicy.AllDescendants);
        }

        private async Task<IAssetHandle> LoadHandleByTypeAsync(Type assetType, AssetKey key, CancellationToken cancellationToken)
        {
            MethodInfo generic = loadByKeyMethod.MakeGenericMethod(assetType);
            Task loadTask = (Task)generic.Invoke(gateway, new object[] { key, cancellationToken });
            await loadTask;
            PropertyInfo resultProperty = loadTask.GetType().GetProperty("Result");
            if (resultProperty == null) { throw new InvalidOperationException("Gateway load task result property could not be resolved."); }
            object handle = resultProperty.GetValue(loadTask);
            if (handle is IAssetHandle typed) { return typed; }
            throw new InvalidOperationException("Gateway load did not return a valid asset handle.");
        }

        private void GuardConstructor(IAddressablesGateway gateway, IAddressablesAssetClient client)
        {
            if (gateway == null) { throw new System.ArgumentNullException(nameof(gateway)); }
            if (client == null) { throw new System.ArgumentNullException(nameof(client)); }
        }

        private void GuardCancellation(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        private static MethodInfo CreateLoadByKeyMethod()
        {
            MethodInfo method = typeof(IAddressablesGateway).GetMethod(nameof(IAddressablesGateway.LoadAsync), new[] { typeof(AssetKey), typeof(CancellationToken) });
            if (method == null) { throw new InvalidOperationException("Unable to resolve gateway key load method."); }
            return method;
        }
    }
}
#pragma warning restore SCA0006
