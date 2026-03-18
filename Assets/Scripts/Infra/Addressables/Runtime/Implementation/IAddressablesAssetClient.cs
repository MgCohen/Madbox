using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Scaffold.Addressables.Contracts;
using UnityEngine;

namespace Scaffold.Addressables
{
    public interface IAddressablesAssetClient
    {
        Task SyncCatalogAndContentAsync(CancellationToken cancellationToken);
        Task<T> LoadAssetAsync<T>(AssetKey key, CancellationToken cancellationToken) where T : UnityEngine.Object;
        Task<IReadOnlyList<AssetKey>> ResolveCatalogAsync(Type assetType, CatalogKey catalog, CancellationToken cancellationToken);
        void Release(UnityEngine.Object asset);
    }
}
