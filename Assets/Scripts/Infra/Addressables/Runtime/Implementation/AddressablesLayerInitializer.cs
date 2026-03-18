using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.Scope.Contracts;
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
            AddressablesPreloadConfigRequestBuilder requestBuilder = new AddressablesPreloadConfigRequestBuilder();
            requestProvider = new AddressablesPreloadRequestProvider(client, requestBuilder);
        }

        private readonly IAddressablesGateway gateway;
        private readonly IAddressablesAssetClient client;
        private readonly AddressablesPreloadRequestProvider requestProvider;
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
            IReadOnlyList<AddressablesPreloadRequest> requests = await requestProvider.LoadRequestsAsync(cancellationToken, "Skipping child preload injection.");
            await RegisterPreloadedAssetsAsync(requests, context, cancellationToken);
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
