using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VContainer;

namespace Madbox.Addressables.Contracts
{
    public interface IAssetProvider
    {
        Task PreloadAsync(CancellationToken cancellationToken);
    }

    public interface IAssetProvider<TAsset> : IAssetProvider where TAsset : UnityEngine.Object
    {
        bool TryGet(out TAsset asset);
    }

    public interface IAssetGroupProvider<TAsset> : IAssetProvider where TAsset : UnityEngine.Object
    {
        bool TryGet(out IReadOnlyList<TAsset> assets);
    }

    public interface IAssetRegistrar
    {
        void Register(IContainerBuilder builder);
    }
}
