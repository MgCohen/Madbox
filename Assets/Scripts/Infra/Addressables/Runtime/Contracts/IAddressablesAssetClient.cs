using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace Madbox.Addressables.Contracts
{
    public interface IAddressablesAssetClient
    {
        Task SyncCatalogAndContentAsync(CancellationToken cancellationToken);
        Task<T> LoadAssetAsync<T>(AssetKey key, CancellationToken cancellationToken) where T : UnityEngine.Object;
        Task<IReadOnlyList<AssetKey>> ResolveLabelAsync(Type assetType, AssetLabelReference label, CancellationToken cancellationToken);
        void Release(UnityEngine.Object asset);
    }
}
