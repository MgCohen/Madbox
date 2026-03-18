using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Scaffold.Addressables.Contracts
{
    public interface IAddressablesGateway
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);
        Task<IAssetHandle<T>> LoadAsync<T>(AssetKey key, CancellationToken cancellationToken = default) where T : UnityEngine.Object;
        Task<IReadOnlyList<IAssetHandle<T>>> LoadAsync<T>(CatalogKey catalog, CancellationToken cancellationToken = default) where T : UnityEngine.Object;
        Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceKey reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object;
    }
}
