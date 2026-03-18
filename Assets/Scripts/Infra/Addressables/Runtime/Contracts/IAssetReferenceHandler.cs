using System;
using System.Threading;
using System.Threading.Tasks;
namespace Madbox.Addressables.Contracts
{
    public interface IAssetReferenceHandler
    {
        Task<IAssetHandle<T>> AcquireAsync<T>(string key, CancellationToken cancellationToken) where T : UnityEngine.Object;
        Task<IAssetHandle> AcquireByTypeAsync(Type assetType, string key, PreloadMode preloadMode, bool isPreload, CancellationToken cancellationToken);
    }
}
