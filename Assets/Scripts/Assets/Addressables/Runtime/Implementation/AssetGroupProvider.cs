using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using UnityEngine.AddressableAssets;
using VContainer;

namespace Madbox.Addressables
{
    public abstract class AssetGroupProvider<TAsset> : IAssetGroupProvider<TAsset>, IAssetRegistrar where TAsset : UnityEngine.Object
    {
        private readonly IAddressablesGateway gateway;
        private readonly List<TAsset> loadedAssets = new List<TAsset>();

        protected AssetGroupProvider(IAddressablesGateway gateway)
        {
            this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        protected abstract AssetLabelReference LabelKey { get; }

        public async Task PreloadAsync(CancellationToken cancellationToken)
        {
            IAssetGroupHandle<TAsset> group = await LoadCoreAsync(cancellationToken);
            loadedAssets.Clear();
            IReadOnlyList<IAssetHandle<TAsset>> handles = group.TypedHandles;
            for (int i = 0; i < handles.Count; i++)
            {
                IAssetHandle<TAsset> handle = handles[i];
                if (handle == null || !handle.IsReady)
                {
                    continue;
                }

                loadedAssets.Add(handle.Asset);
            }
        }

        public bool TryGet(out IReadOnlyList<TAsset> assets)
        {
            assets = loadedAssets;
            return loadedAssets.Count > 0;
        }

        public virtual void Register(IContainerBuilder builder)
        {
            if (builder == null || loadedAssets.Count == 0)
            {
                return;
            }

            builder.RegisterInstance<IReadOnlyList<TAsset>>(loadedAssets);
        }

        protected virtual Task<IAssetGroupHandle<TAsset>> LoadCoreAsync(CancellationToken cancellationToken)
        {
            return gateway.LoadAsync<TAsset>(LabelKey, cancellationToken);
        }
    }
}
